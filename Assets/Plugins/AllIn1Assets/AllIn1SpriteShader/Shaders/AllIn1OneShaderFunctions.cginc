//Prevent Unity warnings that are not relevant for this shader-------------------
#pragma warning (disable : 3571)  // pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them
#pragma warning (disable : 4008)  // floating point division by zero
#pragma warning (disable : 3206)  // implicit truncation of vector type
//BLURS-------------------------------------------------------------------------
float BlurGaussian(float t, float sigma)
{
	return exp(-(t * t) / (2.0 * sigma * sigma));
}

//Spreads Intensity 0 to 100 over the kernel: the previous size at the default 10, close to a flat average at 100
float BlurSigma(half intensity)
{
	return 0.1 + 12.0 * (1.0 - exp(-intensity * 0.054));
}

float SpriteLongestSideTexels(float2 texSize, float2 spriteUVSize)
{
	float2 spriteTexels = texSize * spriteUVSize;
	return max(spriteTexels.x, spriteTexels.y);
}

//xy: UV size of one blur step, 1/256 of the sprite's longest side. z: extra mip levels so wide steps can read a prefiltered mip
float3 GetBlurStep(float2 uv, float4 texelSize, float spriteSideTexels)
{
	float stepTexels = spriteSideTexels / 256.0;
	float2 texelPos = uv * texelSize.zw;
	float2 dx = ddx(texelPos);
	float2 dy = ddy(texelPos);
	float screenLod = 0.5 * log2(max(max(dot(dx, dx), dot(dy, dy)), 1e-8));
	return float3(stepTexels * texelSize.xy, log2(stepTexels) - screenLod);
}

//Alpha weighted, so the colour stored in transparent texels doesn't bleed into the edges
half4 BlurAccumulate(half4 texColor, bool weightByAlpha)
{
	return weightByAlpha ? half4(texColor.rgb * texColor.a, texColor.a) : texColor;
}

half4 BlurResolve(float4 sum, bool weightByAlpha)
{
	return weightByAlpha ? half4(sum.rgb / max(sum.a, 0.0001), sum.a) : half4(sum);
}

//3x3 binomial kernel spread to match BlurHD's variance, so Low Res trades quality for speed without changing the blur size
half4 Blur(half2 uv, sampler2D source, half intensity, float3 blurStep, bool weightByAlpha)
{
	float sigma = BlurSigma(intensity);
	float weightSum = 1.0;
	float varianceSum = 0.0;
	[unroll] for (int t = 1; t <= 8; t++)
	{
		float w = BlurGaussian(t, sigma);
		weightSum += 2.0 * w;
		varianceSum += 2.0 * w * t * t;
	}
	float spread = sqrt(2.0 * varianceSum / weightSum);
	float mipBias = max(0.0, blurStep.z + log2(max(spread * 0.5, 1.0)));

	float4 sum = 0;
	[unroll] for (int y = -1; y <= 1; y++)
	{
		[unroll] for (int x = -1; x <= 1; x++)
		{
			half4 texColor = tex2Dbias(source, float4(uv + float2(x, y) * spread * blurStep.xy, 0, mipBias));
			sum += BlurAccumulate(texColor, weightByAlpha) * ((2 - abs(x)) * (2 - abs(y)) / 16.0);
		}
	}
	return BlurResolve(sum, weightByAlpha);
}

//Gaussian over a disc of radius 8 steps: a 9x9 grid of bilinear fetches, each covering two neighbouring taps, minus its 12 corners
half4 BlurHD(half2 uv, sampler2D source, half intensity, float3 blurStep, bool weightByAlpha)
{
	float sigma = BlurSigma(intensity);
	float offsets[9];
	float weights[9];
	offsets[4] = 0.0;
	weights[4] = 1.0;
	float totalWeight = 1.0;
	[unroll] for (int k = 1; k <= 4; k++)
	{
		float nearWeight = BlurGaussian(2 * k - 1, sigma);
		float farWeight = BlurGaussian(2 * k, sigma);
		float pairWeight = nearWeight + farWeight;
		float pairOffset = ((2 * k - 1) * nearWeight + 2 * k * farWeight) / max(pairWeight, 1e-8);
		offsets[4 + k] = pairOffset;
		offsets[4 - k] = -pairOffset;
		weights[4 + k] = pairWeight;
		weights[4 - k] = pairWeight;
		totalWeight += 2.0 * pairWeight;
	}

	float mipBias = max(0.0, blurStep.z);
	float cornerWeight = 4.0 * weights[8] * weights[8] + 8.0 * weights[7] * weights[8];
	float4 sum = 0;
	[unroll] for (int y = 0; y < 9; y++)
	{
		[unroll] for (int x = 0; x < 9; x++)
		{
			if (abs(x - 4) + abs(y - 4) < 7)
			{
				half4 texColor = tex2Dbias(source, float4(uv + float2(offsets[x], offsets[y]) * blurStep.xy, 0, mipBias));
				sum += BlurAccumulate(texColor, weightByAlpha) * (weights[x] * weights[y]);
			}
		}
	}
	return BlurResolve(sum / (totalWeight * totalWeight - cornerWeight), weightByAlpha);
}
//-----------------------------------------------------------------------
half rand(half2 seed, half offset) {
	return (frac(sin(dot(seed, half2(12.9898, 78.233))) * 43758.5453) + offset) % 1.0;
}

half rand2(half2 seed, half offset, half speed) {
	return (frac(sin(dot(seed * floor(50 + (_Time.x % 1.0) * 12. * speed), half2(127.1, 311.7))) * 43758.5453123) + offset) % 1.0;
}

half rand2CustomTime(half2 seed, half offset, half customTime, half speed) {
	return (frac(sin(dot(seed * floor(50 + (customTime % 1.0) * 12. * speed), half2(127.1, 311.7))) * 43758.5453123) + offset) % 1.0;
}
//-----------------------------------------------------------------------
half RemapFloat(half inValue, half inMin, half inMax, half outMin, half outMax){
	return outMin + (inValue - inMin) * (outMax - outMin) / (inMax - inMin);
}