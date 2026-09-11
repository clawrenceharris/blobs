import Foundation
import CoreGraphics
import ImageIO
import UniformTypeIdentifiers

guard CommandLine.arguments.count == 3 else {
    fatalError("usage: remove_checkerboard.swift input.png output.png")
}

let inputURL = URL(fileURLWithPath: CommandLine.arguments[1]) as CFURL
let outputURL = URL(fileURLWithPath: CommandLine.arguments[2]) as CFURL

guard
    let source = CGImageSourceCreateWithURL(inputURL, nil),
    let sourceImage = CGImageSourceCreateImageAtIndex(source, 0, nil)
else { fatalError("Could not load input image") }

let width = sourceImage.width
let height = sourceImage.height
let bytesPerRow = width * 4
let colorSpace = CGColorSpaceCreateDeviceRGB()
let bitmapInfo = CGImageAlphaInfo.premultipliedLast.rawValue | CGBitmapInfo.byteOrder32Big.rawValue
var pixels = [UInt8](repeating: 0, count: height * bytesPerRow)

pixels.withUnsafeMutableBytes { raw in
    guard let context = CGContext(
        data: raw.baseAddress,
        width: width,
        height: height,
        bitsPerComponent: 8,
        bytesPerRow: bytesPerRow,
        space: colorSpace,
        bitmapInfo: bitmapInfo
    ) else { fatalError("Could not create source context") }
    context.draw(sourceImage, in: CGRect(x: 0, y: 0, width: width, height: height))
}

// The unwanted checkerboard is neutral gray. The sprite is continuously
// colored, so channel separation gives a reliable foreground seed without
// changing any pixels inside the subject.
let chromaThreshold = 18
var candidate = [Bool](repeating: false, count: width * height)
for index in 0..<(width * height) {
    let p = index * 4
    let red = Int(pixels[p])
    let green = Int(pixels[p + 1])
    let blue = Int(pixels[p + 2])
    candidate[index] = max(red, green, blue) - min(red, green, blue) >= chromaThreshold
}

let center = (height / 2) * width + width / 2
guard candidate[center] else { fatalError("Center pixel was not recognized as foreground") }

var foregroundCore = [Bool](repeating: false, count: width * height)
var queue = [Int](repeating: 0, count: width * height)
var head = 0
var tail = 1
queue[0] = center
foregroundCore[center] = true

let neighborOffsets = [(-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1)]
while head < tail {
    let current = queue[head]
    head += 1
    let x = current % width
    let y = current / width
    for (dx, dy) in neighborOffsets {
        let nx = x + dx
        let ny = y + dy
        if nx < 0 || ny < 0 || nx >= width || ny >= height { continue }
        let next = ny * width + nx
        if candidate[next] && !foregroundCore[next] {
            foregroundCore[next] = true
            queue[tail] = next
            tail += 1
        }
    }
}

// Flood the exterior through every pixel outside the connected color core.
// Everything enclosed by that core is part of the gravestone, including its
// low-saturation highlights and engraved details.
var exterior = [Bool](repeating: false, count: width * height)
head = 0
tail = 0
func enqueueExterior(_ index: Int) {
    if !foregroundCore[index] && !exterior[index] {
        exterior[index] = true
        queue[tail] = index
        tail += 1
    }
}
for x in 0..<width {
    enqueueExterior(x)
    enqueueExterior((height - 1) * width + x)
}
for y in 0..<height {
    enqueueExterior(y * width)
    enqueueExterior(y * width + width - 1)
}

let floodOffsets = [(-1, 0), (1, 0), (0, -1), (0, 1)]
while head < tail {
    let current = queue[head]
    head += 1
    let x = current % width
    let y = current / width
    for (dx, dy) in floodOffsets {
        let nx = x + dx
        let ny = y + dy
        if nx < 0 || ny < 0 || nx >= width || ny >= height { continue }
        let next = ny * width + nx
        if !foregroundCore[next] && !exterior[next] {
            exterior[next] = true
            queue[tail] = next
            tail += 1
        }
    }
}

var opaqueCount = 0
var minX = width
var minY = height
var maxX = 0
var maxY = 0
for index in 0..<(width * height) {
    let p = index * 4
    if exterior[index] {
        pixels[p] = 0
        pixels[p + 1] = 0
        pixels[p + 2] = 0
        pixels[p + 3] = 0
    } else {
        pixels[p + 3] = 255
        opaqueCount += 1
        let x = index % width
        let y = index / width
        minX = min(minX, x)
        minY = min(minY, y)
        maxX = max(maxX, x)
        maxY = max(maxY, y)
    }
}

guard let provider = CGDataProvider(data: Data(pixels) as CFData),
      let cutout = CGImage(
        width: width,
        height: height,
        bitsPerComponent: 8,
        bitsPerPixel: 32,
        bytesPerRow: bytesPerRow,
        space: colorSpace,
        bitmapInfo: CGBitmapInfo(rawValue: bitmapInfo),
        provider: provider,
        decode: nil,
        shouldInterpolate: true,
        intent: .defaultIntent
      ) else { fatalError("Could not build cutout") }

let outputSize = 526
let outputBytesPerRow = outputSize * 4
var outputPixels = [UInt8](repeating: 0, count: outputSize * outputBytesPerRow)
outputPixels.withUnsafeMutableBytes { raw in
    guard let context = CGContext(
        data: raw.baseAddress,
        width: outputSize,
        height: outputSize,
        bitsPerComponent: 8,
        bytesPerRow: outputBytesPerRow,
        space: colorSpace,
        bitmapInfo: bitmapInfo
    ) else { fatalError("Could not create output context") }
    context.interpolationQuality = .high
    context.clear(CGRect(x: 0, y: 0, width: outputSize, height: outputSize))
    context.draw(cutout, in: CGRect(x: 0, y: 0, width: outputSize, height: outputSize))
}

// Remove a handful of yellow-gray fringe pixels introduced where the generated
// checker pattern touched the purple outline. These hues do not occur in the
// blue-purple stone or the green moss.
var fringeCount = 0
for index in 0..<(outputSize * outputSize) {
    let p = index * 4
    let red = Int(outputPixels[p])
    let green = Int(outputPixels[p + 1])
    let blue = Int(outputPixels[p + 2])
    let alpha = Int(outputPixels[p + 3])
    let unpremultipliedRed = alpha > 0 ? min(255, red * 255 / alpha) : 0
    let unpremultipliedGreen = alpha > 0 ? min(255, green * 255 / alpha) : 0
    let unpremultipliedBlue = alpha > 0 ? min(255, blue * 255 / alpha) : 0
    if alpha > 0 && abs(unpremultipliedRed - unpremultipliedGreen) <= 30
        && unpremultipliedRed - unpremultipliedBlue >= 35
        && unpremultipliedGreen - unpremultipliedBlue >= 35 {
        outputPixels[p] = 0
        outputPixels[p + 1] = 0
        outputPixels[p + 2] = 0
        outputPixels[p + 3] = 0
        fringeCount += 1
    }
}

guard let outputProvider = CGDataProvider(data: Data(outputPixels) as CFData),
      let outputImage = CGImage(
        width: outputSize,
        height: outputSize,
        bitsPerComponent: 8,
        bitsPerPixel: 32,
        bytesPerRow: outputBytesPerRow,
        space: colorSpace,
        bitmapInfo: CGBitmapInfo(rawValue: bitmapInfo),
        provider: outputProvider,
        decode: nil,
        shouldInterpolate: true,
        intent: .defaultIntent
      ),
      let destination = CGImageDestinationCreateWithURL(outputURL, UTType.png.identifier as CFString, 1, nil)
else { fatalError("Could not create output image") }

CGImageDestinationAddImage(destination, outputImage, nil)
guard CGImageDestinationFinalize(destination) else { fatalError("Could not save output PNG") }

let alphaIndices = stride(from: 3, to: outputPixels.count, by: 4)
let transparentCount = alphaIndices.filter { outputPixels[$0] == 0 }.count
let partialCount = alphaIndices.filter { outputPixels[$0] > 0 && outputPixels[$0] < 255 }.count
print("source foreground: \(opaqueCount) pixels, bounds (\(minX),\(minY))-(\(maxX),\(maxY))")
print("output: \(outputSize)x\(outputSize), transparent: \(transparentCount), antialiased: \(partialCount), fringe removed: \(fringeCount)")
