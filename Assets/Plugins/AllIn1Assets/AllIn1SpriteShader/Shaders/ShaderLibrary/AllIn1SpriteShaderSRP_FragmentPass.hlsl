#ifndef ALLIN1SPRITESHADERSRP_FRAGMENTPASS
#define ALLIN1SPRITESHADERSRP_FRAGMENTPASS

half4 frag(v2f i) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                //half randomSeed = UNITY_ACCESS_INSTANCED_PROP(Props, _RandomSeed);
	float randomSeed = _RandomSeed;

	float texWidth;
	float texHeight;
	_MainTex.GetDimensions(texWidth, texHeight);
	float4 texelSize = float4(1.0 / texWidth, 1 / texHeight, texWidth, texHeight);

	float2 uvRect = i.uv;
	half2 center = half2(0.5, 0.5);
#if ATLAS_ON
	center = half2((_MaxXUV + _MinXUV) / 2.0, (_MaxYUV + _MinYUV) / 2.0);
	uvRect = half2((i.uv.x - _MinXUV) / (_MaxXUV - _MinXUV), (i.uv.y - _MinYUV) / (_MaxYUV - _MinYUV));
#endif
	half2 centerTiled = half2(center.x * _MainTex_ScaleAndTiling.x, center.y * _MainTex_ScaleAndTiling.y);

#if CLIPPING_ON
	half2 tiledUv = half2(i.uv.x / _MainTex_ScaleAndTiling.x, i.uv.y / _MainTex_ScaleAndTiling.y);
#if ATLAS_ON
	tiledUv = half2((tiledUv.x - _MinXUV) / (_MaxXUV - _MinXUV), (tiledUv.y - _MinYUV) / (_MaxYUV - _MinYUV));
#endif
	clip((1 - _ClipUvUp) - tiledUv.y);
	clip(tiledUv.y - _ClipUvDown);
	clip((1 - _ClipUvRight) - tiledUv.x);
	clip(tiledUv.x - _ClipUvLeft);
#endif

#if RADIALCLIPPING_ON
	half2 tiledUv2 = half2(i.uv.x / _MainTex_ScaleAndTiling.x, i.uv.y / _MainTex_ScaleAndTiling.y);
#if ATLAS_ON
	tiledUv2 = half2((tiledUv2.x - _MinXUV) / (_MaxXUV - _MinXUV), (tiledUv2.y - _MinYUV) / (_MaxYUV - _MinYUV));
#endif
	half startAngle = _RadialStartAngle - _RadialClip;
    half endAngle = _RadialStartAngle + _RadialClip2;
    half offset0 = clamp(0, 360, startAngle + 360);
    half offset360 = clamp(0, 360, endAngle - 360);
    half2 atan2Coord = half2(lerp(-1, 1, tiledUv2.x), lerp(-1, 1, tiledUv2.y));
    half atanAngle = atan2(atan2Coord.y, atan2Coord.x) * 57.3; // angle in degrees
    if(atanAngle < 0) atanAngle = 360 + atanAngle;
    if(atanAngle >= startAngle && atanAngle <= endAngle) discard;
    if(atanAngle <= offset360) discard;
    if(atanAngle >= offset0) discard;
#endif

#if TEXTURESCROLL_ON && ATLAS_ON
	i.uv = half2(_MinXUV + ((_MaxXUV - _MinXUV) * (abs(((_Time.y + randomSeed) * _TextureScrollXSpeed) + uvRect.x) % 1)),
	_MinYUV + ((_MaxYUV - _MinYUV) * (abs(((_Time.y + randomSeed) * _TextureScrollYSpeed) + uvRect.y) % 1)));
#endif

#if OFFSETUV_ON
#if ATLAS_ON
	i.uv = half2(_MinXUV + ((_MaxXUV - _MinXUV) * (abs((_OffsetUvX + uvRect.x) % 1))),
	_MinYUV + ((_MaxYUV - _MinYUV) * (abs(_OffsetUvY + uvRect.y) % 1)));
#else
	i.uv += half2(_OffsetUvX, _OffsetUvY);
#endif
#endif

#if POLARUV_ON
	i.uv = half2(atan2(i.uv.y, i.uv.x) / (2.0f * 3.141592653589f), length(i.uv));
	i.uv *= _MainTex_ScaleAndTiling.xy;
#endif

#if TWISTUV_ON
	half twistUvPosX = _TwistUvPosX;
	half twistUvPosY = _TwistUvPosY;
#if ATLAS_ON
	twistUvPosX = ((_MaxXUV - _MinXUV) * twistUvPosX) + _MinXUV;
	twistUvPosY = ((_MaxYUV - _MinYUV) * twistUvPosY) + _MinYUV;
#endif
	half2 tempUv = i.uv - half2(twistUvPosX *  _MainTex_ScaleAndTiling.x, twistUvPosY *  _MainTex_ScaleAndTiling.y);
	half twistUvRadius = _TwistUvRadius * ((_MainTex_ScaleAndTiling.x + _MainTex_ScaleAndTiling.y) / 2);
	half percent = saturate((twistUvRadius - length(tempUv)) / max(twistUvRadius, 0.0001));
	half theta = percent * percent * (2.0 * sin(_TwistUvAmount)) * 8.0;
	half s = sin(theta);
	half c = cos(theta);
	half beta = max(sign(twistUvRadius - length(tempUv)), 0.0);
	tempUv = half2(dot(tempUv, half2(c, -s)), dot(tempUv, half2(s, c))) * beta +	tempUv * (1 - beta);
	tempUv += half2(twistUvPosX *  _MainTex_ScaleAndTiling.x, twistUvPosY *  _MainTex_ScaleAndTiling.y);
	i.uv = tempUv;
#endif

#if FISHEYE_ON
	half bind = length(centerTiled);
	half2 dF = i.uv - centerTiled;
	half dFlen = length(dF);
	half fishInt = (3.14159265359 / bind) * (_FishEyeUvAmount + 0.001);
	i.uv = centerTiled + (dF / (max(0.0001, dFlen))) * tan(dFlen * fishInt) * bind / tan(bind * fishInt);
#endif

#if PINCH_ON
	half2 dP = i.uv - centerTiled;
	half pinchInt = (3.141592 / length(centerTiled)) * (-_PinchUvAmount - 0.001);
	i.uv = centerTiled + (dP / max(0.0001, length(dP))) * atan(length(dP) * -pinchInt * 10.0) * 0.5 / atan(-pinchInt * 5);
#endif

#if ZOOMUV_ON
	i.uv -= centerTiled;
	i.uv = i.uv * _ZoomUvAmount;
	i.uv += centerTiled;
#endif

#if DOODLE_ON
	half2 uvCopy = uvRect;
	//fmod by tau/4 (the *4 below makes it a full 2*PI period) keeps the value bounded so it never overflows to inf in half precision, which turns the uv into nan
	float handDrawnTime = fmod(floor((_Time.x + randomSeed) * 20 * _HandDrawnSpeed), 1.57079632679);
	uvCopy.x = sin((uvCopy.x * _HandDrawnAmount + handDrawnTime) * 4);
	uvCopy.y = cos((uvCopy.y * _HandDrawnAmount + handDrawnTime) * 4);
	i.uv = lerp(i.uv, i.uv + uvCopy, 0.0005 * _HandDrawnAmount);
#endif
            	
#if SHAKEUV_ON
	//fmod by 2*PI keeps the trig input bounded: unbounded time values eventually lose precision or overflow on some platforms
	float shakeTime = fmod((_Time.x + randomSeed) * _ShakeUvSpeed * 50, 6.28318530718);
	half xShake = sin(shakeTime) * _ShakeUvX;
	half yShake = cos(shakeTime) * _ShakeUvY;
	i.uv += half2(xShake * 0.012, yShake * 0.01);
#endif

#if RECTSIZE_ON
	i.uv = i.uv.xy * (_RectSize).xx + (((-_RectSize * 0.5) + 0.5)).xx;
#endif

#if DISTORT_ON
#if ATLAS_ON
    i.uvDistTex.x = i.uvDistTex.x * (1 / (_MaxXUV - _MinXUV));
	i.uvDistTex.y = i.uvDistTex.y * (1 / (_MaxYUV - _MinYUV));
#endif
    i.uvDistTex.x += ((_Time.x + randomSeed) * _DistortTexXSpeed) % 1;
	i.uvDistTex.y += ((_Time.x + randomSeed) * _DistortTexYSpeed) % 1;
	half distortAmnt = (SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, i.uvDistTex).r - 0.5) * 0.2 * _DistortAmount;
	i.uv.x += distortAmnt;
	i.uv.y += distortAmnt;
#endif

#if WARP_ON
	half2 warpUv = half2(i.uv.x / _MainTex_ScaleAndTiling.x, i.uv.y / _MainTex_ScaleAndTiling.y);
#if ATLAS_ON
	warpUv = half2((warpUv.x - _MinXUV) / (_MaxXUV - _MinXUV), (warpUv.y - _MinYUV) / (_MaxYUV - _MinYUV));
#endif
	const float tau = 6.283185307179586;
	//fmod by tau keeps the trig input bounded: unbounded time values eventually lose precision or overflow on some platforms
	float warpTime = fmod((_Time.y + randomSeed) * _WarpSpeed, tau);
    float xWarp = warpTime + warpUv.x * tau / _WarpScale;
    float yWarp = warpTime + warpUv.y * tau / _WarpScale;
    float2 warp = float2(sin(xWarp), sin(yWarp)) * _WarpStrength;
    i.uv += warp;
#endif

#if WAVEUV_ON
	float2 uvWave = half2(_WaveX * _MainTex_ScaleAndTiling.x, _WaveY * _MainTex_ScaleAndTiling.y) - i.uv;
    uvWave %= 1;
#if ATLAS_ON
	uvWave = half2(_WaveX, _WaveY) - uvRect;
#endif
	uvWave.x *= _ScreenParams.x / _ScreenParams.y;
	//fmod by 2*PI keeps the trig input bounded: unbounded time values eventually lose precision or overflow on some platforms
    float waveTime = fmod((_Time.y + randomSeed) * _WaveSpeed, 6.28318530718);
	float angWave = (sqrt(dot(uvWave, uvWave)) * _WaveAmount) - waveTime;
	i.uv = i.uv + uvWave * sin(angWave) * (_WaveStrength / 1000.0);
#endif

#if ROUNDWAVEUV_ON
	half xWave = ((0.5 * _MainTex_ScaleAndTiling.x) - uvRect.x);
	half yWave = ((0.5 * _MainTex_ScaleAndTiling.y) - uvRect.y) * (texelSize.w / texelSize.z);
	half ripple = -sqrt(xWave*xWave + yWave* yWave);
	//fmod by 2*PI keeps the trig input bounded: unbounded time values eventually lose precision or overflow on some platforms
	float roundWaveTime = fmod((_Time.y + randomSeed) * (_RoundWaveSpeed/10.0) / 0.015, 6.28318530718);
	i.uv += (sin((ripple / 0.015) + roundWaveTime) * (_RoundWaveStrength/10.0)) % 1;
#endif

#if WIND_ON
	//fmod by 2*PI keeps the trig input bounded: unbounded time values eventually lose precision or overflow on some platforms
	half windOffset = sin(fmod((_Time.x + randomSeed) * _GrassSpeed * 10, 6.28318530718));
	half2 windCenter = half2(0.5, 0.1);
#if ATLAS_ON
	windCenter.x = ((_MaxXUV - _MinXUV) * windCenter.x) + _MinXUV;
	windCenter.y = ((_MaxYUV - _MinYUV) * windCenter.y) + _MinYUV;
#endif
#if !MANUALWIND_ON
	i.uv.x = fmod(abs(lerp(i.uv.x, i.uv.x + (_GrassWind * 0.01 * windOffset), uvRect.y)), 1);
#else
	i.uv.x = fmod(abs(lerp(i.uv.x, i.uv.x + (_GrassWind * 0.01 * _GrassManualAnim), uvRect.y)), 1);
	windOffset = _GrassManualAnim;
#endif
	half2 delta = i.uv - windCenter;
	half delta2 = dot(delta.xy, delta.xy);
	half2 delta_offset = delta2 * windOffset;
	i.uv = i.uv + half2(delta.y, -delta.x) * delta_offset * _GrassRadialBend;
#endif

#if TEXTURESCROLL_ON && !ATLAS_ON
	i.uv.x += ((_Time.y + randomSeed) * _TextureScrollXSpeed) % 1;
	i.uv.y += ((_Time.y + randomSeed) * _TextureScrollYSpeed) % 1;
#endif

#if PIXELATE_ON
    half aspectRatio = texelSize.x / texelSize.y;
	half2 pixelSize = float2(_PixelateSize, _PixelateSize * aspectRatio);
	i.uv = floor(i.uv * pixelSize) / pixelSize;
#endif

	half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
	half originalAlpha = col.a;
#if BLUR_ON || MOTIONBLUR_ON
#if ATLAS_ON
	float spriteSideTexels = SpriteLongestSideTexels(texelSize.zw, float2(_MaxXUV - _MinXUV, _MaxYUV - _MinYUV));
#else
	float spriteSideTexels = SpriteLongestSideTexels(texelSize.zw, float2(1, 1));
#endif
#endif
#if BLUR_ON
	float3 blurStep = GetBlurStep(i.uv, texelSize, spriteSideTexels);
#if !BLURISHD_ON
#define SAMPLE_MAINTEX_WITH_BLUR(uv) BlurHD(uv, _MainTex, sampler_MainTex, _BlurIntensity, blurStep, true)
#else
#define SAMPLE_MAINTEX_WITH_BLUR(uv) Blur(uv, _MainTex, sampler_MainTex, _BlurIntensity, blurStep, true)
#endif
	col = SAMPLE_MAINTEX_WITH_BLUR(i.uv);
#else
#define SAMPLE_MAINTEX_WITH_BLUR(uv) SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv)
#endif
	col *= i.color;
#if PREMULTIPLYALPHA_ON
	col.rgb *= col.a;
#endif

#if GLITCH_ON
	half2 uvGlitch = uvRect;
	uvGlitch.y -= 0.5;
	half lineNoise = pow(rand2(floor(uvGlitch * half2(24., 19.) * _GlitchSize) * 4.0, randomSeed, _GlitchSpeed), 3.0) * _GlitchAmount
		* pow(rand2(floor(uvGlitch * half2(38., 14.) * _GlitchSize) * 4.0, randomSeed, _GlitchSpeed), 3.0);
	col = SAMPLE_MAINTEX_WITH_BLUR(i.uv + half2(lineNoise * 0.02 * rand2(half2(2.0, 1), randomSeed, _GlitchSpeed), 0)) * i.color;
#endif

#if CHROMABERR_ON
	half4 r = SAMPLE_MAINTEX_WITH_BLUR(i.uv + half2(_ChromAberrAmount/10, 0)) * i.color;
	half4 b = SAMPLE_MAINTEX_WITH_BLUR(i.uv + half2(-_ChromAberrAmount/10, 0)) * i.color; 
	col = half4(r.r * r.a, col.g, b.b * b.a, max(max(r.a, b.a) * _ChromAberrAlpha, col.a));
#endif

#if MOTIONBLUR_ON
	half motionBlurAngle = _MotionBlurAngle * 3.1415926;
	half2 motionBlurDir = mul(half2(1, 1), half2x2(cos(motionBlurAngle), -sin(motionBlurAngle), sin(motionBlurAngle), cos(motionBlurAngle)));
	float2 motionBlurStep = motionBlurDir * (_MotionBlurDist * 0.005 * spriteSideTexels) * texelSize.xy;
#if PREMULTIPLYALPHA_ON
	half4 motionBlurSum = col;
#else
	half4 motionBlurSum = half4(col.rgb * col.a, col.a);
#endif
	[unroll] for (int motionTap = 1; motionTap <= 4; motionTap++)
	{
		half4 motionForward = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + motionBlurStep * motionTap) * i.color;
		half4 motionBack = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - motionBlurStep * motionTap) * i.color;
		motionBlurSum += half4(motionForward.rgb * motionForward.a, motionForward.a) + half4(motionBack.rgb * motionBack.a, motionBack.a);
	}
	motionBlurSum /= 9.0;
#if PREMULTIPLYALPHA_ON
	col = motionBlurSum;
#else
	col = half4(motionBlurSum.rgb / max(motionBlurSum.a, 0.0001), motionBlurSum.a);
#endif
#endif

#if NEGATIVE_ON
	col.rgb = lerp(col.rgb, 1 - col.rgb, _NegativeAmount);
#endif

	half luminance = 0;
#if GREYSCALE_ON && !GREYSCALEOUTLINE_ON
	luminance = 0.3 * col.r + 0.59 * col.g + 0.11 * col.b;
	luminance = saturate(luminance + _GreyscaleLuminosity);
	col.rgb = lerp(col.rgb, half3(luminance, luminance, luminance) * _GreyscaleTintColor, _GreyscaleBlend);
#endif

#if GHOST_ON
	luminance = 0.3 * col.r + 0.59 * col.g + 0.11 * col.b;
	half4 ghostResult;
	ghostResult.a = saturate(luminance - _GhostTransparency) * col.a;
	ghostResult.rgb = col.rgb * (luminance + _GhostColorBoost);
	col = lerp(col, ghostResult, _GhostBlend);
#endif

#if INNEROUTLINE_ON
	half3 innerT = abs(GET_PIXEL(0, _InnerOutlineThickness, i.uv, _MainTex, texelSize) - GET_PIXEL(0, -_InnerOutlineThickness, i.uv, _MainTex, texelSize));
	innerT += abs(GET_PIXEL(_InnerOutlineThickness, 0, i.uv, _MainTex, texelSize) - GET_PIXEL(-_InnerOutlineThickness, 0, i.uv, _MainTex, texelSize));
#if !ONLYINNEROUTLINE_ON
	innerT = (innerT / 2.0) * col.a * _InnerOutlineAlpha;
	col.rgb += length(innerT) * _InnerOutlineColor.rgb * _InnerOutlineGlow;
#else
	innerT *= col.a * _InnerOutlineAlpha;
	col.rgb = length(innerT) * _InnerOutlineColor.rgb * _InnerOutlineGlow;
	col.a = step(0.3, col.r+col.g+col.b);
#endif
#endif

#if HITEFFECT_ON
	col.rgb = lerp(col.rgb, _HitEffectColor.rgb * _HitEffectGlow, _HitEffectBlend);
#endif

#if GRADIENT_ON
	half2 tiledUvGrad = half2(uvRect.x / _MainTex_ScaleAndTiling.x, uvRect.y / _MainTex_ScaleAndTiling.y);
	half4 gradTopRightCol = _GradTopRightCol;
	half4 gradBotRightCol = _GradBotRightCol;
#if GRADIENT2COL_ON
	gradTopRightCol = _GradTopLeftCol;
	gradBotRightCol = _GradBotLeftCol;
#endif
#if RADIALGRADIENT_ON
	half radialDist = 1 - length(tiledUvGrad - half2(0.5, 0.5));
	radialDist *= (texelSize.w / texelSize.z);
	radialDist = saturate(_GradBoostX * radialDist);
	half4 gradientResult = lerp(_GradTopLeftCol, _GradBotLeftCol, radialDist);
#else
	half gradXLerpFactor = saturate(pow(tiledUvGrad.x, _GradBoostX));
	half4 gradientResult = lerp(lerp(_GradBotLeftCol, gradBotRightCol, gradXLerpFactor),
	lerp(_GradTopLeftCol, gradTopRightCol, gradXLerpFactor), saturate(pow(tiledUvGrad.y, _GradBoostY)));
#endif
	gradientResult = lerp(col, gradientResult, _GradBlend);
	col.rgb = gradientResult.rgb * col.a;
	col.a *= gradientResult.a;
#endif

#if CONTRAST_ON
	col.rgb = (col.rgb - float3(0.5, 0.5, 0.5)) * _Contrast + float3(0.5, 0.5, 0.5);
	col.rgb += _Brightness;
#endif

#if COLORSWAP_ON
	luminance = 0.3 * col.r + 0.59 * col.g + 0.11 * col.b;
	half4 swapMask = SAMPLE_TEXTURE2D(_ColorSwapTex, sampler_ColorSwapTex, i.uv);
	swapMask.rgb *= swapMask.a;
	half3 redSwap = _ColorSwapRed * swapMask.r * saturate(luminance + _ColorSwapRedLuminosity);
	half3 greenSwap = _ColorSwapGreen * swapMask.g * saturate(luminance + _ColorSwapGreenLuminosity);
	half3 blueSwap = _ColorSwapBlue * swapMask.b * saturate(luminance + _ColorSwapBlueLuminosity);
	swapMask.rgb = col.rgb * saturate(1 - swapMask.r - swapMask.g - swapMask.b);
	col.rgb = lerp(col.rgb, swapMask.rgb + redSwap + greenSwap + blueSwap, _ColorSwapBlend);
#endif

#if COLORRAMP_ON && !COLORRAMPOUTLINE_ON 
	luminance = 0.3 * col.r + 0.59 * col.g + 0.11 * col.b;
	luminance = saturate(luminance + _ColorRampLuminosity);
#if GRADIENTCOLORRAMP_ON
	col.rgb = lerp(col.rgb, SAMPLE_TEXTURE2D(_ColorRampTexGradient, sampler_ColorRampTexGradient, half2(luminance, 0)).rgb, _ColorRampBlend);
#else
	col.rgb = lerp(col.rgb, SAMPLE_TEXTURE2D(_ColorRampTex, sampler_ColorRampTex, half2(luminance, 0)).rgb, _ColorRampBlend);
#endif
#endif

#if CHANGECOLOR_ON
	float3 currChangeColor = saturate(col.rgb);
	luminance = 0.3 * currChangeColor.r + 0.59 * currChangeColor.g + 0.11 * currChangeColor.b;
	luminance = saturate(luminance + _ColorChangeLuminosity);
	half3 dif = abs(currChangeColor - _ColorChangeTarget.rgb);
	col.rgb = lerp(col.rgb, half3(luminance, luminance, luminance) * _ColorChangeNewCol.rgb,
		max(sign(1 - saturate(dif.x + dif.y + dif.z) - _ColorChangeTolerance), 0.0));
#if CHANGECOLOR2_ON
	dif = abs(currChangeColor - _ColorChangeTarget2.rgb);
	col.rgb = lerp(col.rgb, half3(luminance, luminance, luminance) * _ColorChangeNewCol2.rgb,
		max(sign(1 - saturate(dif.x + dif.y + dif.z) - _ColorChangeTolerance2), 0.0));
#endif
#if CHANGECOLOR3_ON
	dif = abs(currChangeColor - _ColorChangeTarget3.rgb);
	col.rgb = lerp(col.rgb, half3(luminance, luminance, luminance) * _ColorChangeNewCol3.rgb,
		max(sign(1 - saturate(dif.x + dif.y + dif.z) - _ColorChangeTolerance3), 0.0));
#endif
#endif

#if POSTERIZE_ON && !POSTERIZEOUTLINE_ON
	col.rgb = pow(col.rgb, _PosterizeGamma) * _PosterizeNumColors;
	col.rgb = floor(col.rgb) / max(_PosterizeNumColors, 0.0001);
	col.rgb = pow(col.rgb, 1.0 / _PosterizeGamma);
#endif

#if HSV_ON
	half3 resultHsv = half3(col.rgb);
	half cosHsv = _HsvBright * _HsvSaturation * cos(_HsvShift * 3.14159265 / 180);
	half sinHsv = _HsvBright * _HsvSaturation * sin(_HsvShift * 3.14159265 / 180);
	resultHsv.x = (.299 * _HsvBright + .701 * cosHsv + .168 * sinHsv) * col.x
		+ (.587 * _HsvBright - .587 * cosHsv + .330 * sinHsv) * col.y
		+ (.114 * _HsvBright - .114 * cosHsv - .497 * sinHsv) * col.z;
	resultHsv.y = (.299 * _HsvBright - .299 * cosHsv - .328 * sinHsv) *col.x
		+ (.587 * _HsvBright + .413 * cosHsv + .035 * sinHsv) * col.y
		+ (.114 * _HsvBright - .114 * cosHsv + .292 * sinHsv) * col.z;
	resultHsv.z = (.299 * _HsvBright - .3 * cosHsv + 1.25 * sinHsv) * col.x
		+ (.587 * _HsvBright - .588 * cosHsv - 1.05 * sinHsv) * col.y
		+ (.114 * _HsvBright + .886 * cosHsv - .203 * sinHsv) * col.z;
	col.rgb = resultHsv;
#endif

#if OVERLAY_ON
    half2 overlayUvs = uvRect;
    overlayUvs.x += ((_Time.y + randomSeed) * _OverlayTextureScrollXSpeed) % 1;
	overlayUvs.y += ((_Time.y + randomSeed) * _OverlayTextureScrollYSpeed) % 1;
	half4 overlayCol = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, CUSTOM_TRANSFORM_TEX(overlayUvs, _OverlayTex_ScaleAndTiling));
	overlayCol.rgb *= _OverlayColor.rgb * _OverlayGlow;
#if !OVERLAYMULT_ON
	overlayCol.rgb *= overlayCol.a * _OverlayColor.rgb * _OverlayColor.a * _OverlayBlend;
	col.rgb += overlayCol.rgb;
#else
	overlayCol.a *= _OverlayColor.a;
	col = lerp(col, col * overlayCol, _OverlayBlend);
#endif
#endif

				//OUTLINE-------------------------------------------------------------
#if OUTBASE_ON
	#if OUTBASEPIXELPERF_ON
		//half2 destUv = _OutlinePixelWidth / unity_SpriteProps.w;
		half2 destUv = half2(_OutlinePixelWidth * texelSize.x, _OutlinePixelWidth * texelSize.y);
	#else
		//half2 destUv = _OutlinePixelWidth / unity_SpriteProps.w;
		half2 destUv = half2(_OutlineWidth * texelSize.x * 200, _OutlineWidth * texelSize.y * 200);
	#endif  

	#if OUTDIST_ON
		half2 uvOutDist = uvRect;
		uvOutDist.x += ((_Time.x + randomSeed) * _OutlineDistortTexXSpeed) % 1;
		uvOutDist.y += ((_Time.x + randomSeed) * _OutlineDistortTexYSpeed) % 1;
		half outDistortAmnt = (SAMPLE_TEXTURE2D(_OutlineDistortTex, sampler_OutlineDistortTex, CUSTOM_TRANSFORM_TEX(uvOutDist, _OutlineDistortTex_ScaleAndTiling)).r - 0.5) * 0.2 * _OutlineDistortAmount;
		destUv.x += outDistortAmnt;
		destUv.y += outDistortAmnt;
	#endif

		half spriteLeft		= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + half2(destUv.x, 0)).a;
		half spriteRight	= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - half2(destUv.x, 0)).a;
		half spriteBottom	= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + half2(0, destUv.y)).a;
		half spriteTop		= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - half2(0, destUv.y)).a;

		half result = spriteLeft + spriteRight + spriteBottom + spriteTop;

	#if OUTBASE8DIR_ON
		half spriteTopLeft	= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + half2(destUv.x, destUv.y)).a;
		half spriteTopRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + half2(-destUv.x, destUv.y)).a;
		half spriteBotLeft	= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + half2(destUv.x, -destUv.y)).a;
		half spriteBotRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + half2(-destUv.x, -destUv.y)).a;

		result = result + spriteTopLeft + spriteTopRight + spriteBotLeft + spriteBotRight;
	#endif
					
		result = step(0.05, saturate(result));

	half4 outlineColor = _OutlineColor;
	#if OUTTEX_ON
		i.uvOutTex.x += ((_Time.x + randomSeed) * _OutlineTexXSpeed) % 1;
		i.uvOutTex.y += ((_Time.x + randomSeed) * _OutlineTexYSpeed) % 1;
	#if ATLAS_ON
		i.uvOutTex = half2((i.uvOutTex.x - _MinXUV) / (_MaxXUV - _MinXUV), (i.uvOutTex.y - _MinYUV) / (_MaxYUV - _MinYUV));
	#endif 
		half4 tempOutColor = SAMPLE_TEXTURE2D(_OutlineTex, sampler_OutlineTex, i.uvOutTex);
		tempOutColor *= _OutlineColor;
		outlineColor = tempOutColor;
	#endif

		result *= (1 - originalAlpha) * _OutlineAlpha;

		half4 outline = outlineColor * i.color.a;
		outline.rgb *= _OutlineGlow;
		outline.a = result;
	#if ONLYOUTLINE_ON
		col = outline;
	#else
		col = lerp(col, outline, result);
	#endif
#endif
	//-----------------------------------------------------------------------------

#if FADE_ON
	half2 tiledUvFade1= CUSTOM_TRANSFORM_TEX(i.uv, _FadeTex_ScaleAndTiling);
	half2 tiledUvFade2 = CUSTOM_TRANSFORM_TEX(i.uv, _FadeBurnTex_ScaleAndTiling);
#if ATLAS_ON
	tiledUvFade1 = half2((tiledUvFade1.x - _MinXUV) / (_MaxXUV - _MinXUV), (tiledUvFade1.y - _MinYUV) / (_MaxYUV - _MinYUV));
	tiledUvFade2 = half2((tiledUvFade2.x - _MinXUV) / (_MaxXUV - _MinXUV), (tiledUvFade2.y - _MinYUV) / (_MaxYUV - _MinYUV));
#endif
	half fadeTemp = SAMPLE_TEXTURE2D(_FadeTex, sampler_FadeTex, tiledUvFade1).r;
	half fade = smoothstep(_FadeAmount, _FadeAmount + _FadeBurnTransition, fadeTemp);
	half fadeBurn = saturate(smoothstep(_FadeAmount - _FadeBurnWidth, _FadeAmount - _FadeBurnWidth + 0.1, fadeTemp) * _FadeAmount);
	col.a *= fade;
	half4 fadeBurnColor = half4(_FadeBurnColor.rgb * _FadeBurnGlow, _FadeBurnColor.a);
	col += fadeBurn * SAMPLE_TEXTURE2D(_FadeBurnTex, sampler_FadeBurnTex, tiledUvFade2) * fadeBurnColor * originalAlpha * (1 - col.a);
#endif
				
#if SHADOW_ON
	half shadowA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + half2(_ShadowX, _ShadowY)).a;
	half preMultShadowMask = 1 - (saturate(shadowA - col.a) * (1 - col.a));
	col.rgb *= 1 - ((shadowA - col.a) * (1 - col.a));
	col.rgb += (_ShadowColor * shadowA) * (1 - col.a);
	col.a = max(shadowA * _ShadowAlpha * i.color.a, col.a);
#endif

#if GLOW_ON
	half4 emission;
#if GLOWTEX_ON
	emission = SAMPLE_TEXTURE2D(_GlowTex, sampler_GlowTex, i.uv);
#else
	emission = col;
#endif
    col.rgb *= _GlowGlobal;
	emission.rgb *= emission.a * col.a * _Glow * _GlowColor;
	col.rgb += emission.rgb;
#endif

#if COLORRAMP_ON && COLORRAMPOUTLINE_ON 
	luminance = 0.3 * col.r + 0.59 * col.g + 0.11 * col.b;
	luminance = saturate(luminance + _ColorRampLuminosity);
#if GRADIENTCOLORRAMP_ON
	col.rgb = lerp(col.rgb, tex2D(_ColorRampTexGradient, half2(luminance, 0)).rgb, _ColorRampBlend);
#else
	col.rgb = lerp(col.rgb, tex2D(_ColorRampTex, half2(luminance, 0)).rgb, _ColorRampBlend);
#endif
#endif

#if GREYSCALE_ON && GREYSCALEOUTLINE_ON
	luminance = 0.3 * col.r + 0.59 * col.g + 0.11 * col.b;
	luminance = saturate(luminance + _GreyscaleLuminosity);
	col.rgb = lerp(col.rgb, half3(luminance, luminance, luminance) * _GreyscaleTintColor, _GreyscaleBlend);
#endif

#if POSTERIZE_ON && POSTERIZEOUTLINE_ON
	col.rgb = pow(col.rgb, _PosterizeGamma) * _PosterizeNumColors;
	col.rgb = floor(col.rgb) / max(_PosterizeNumColors, 0.0001);
	col.rgb = pow(col.rgb, 1.0 / _PosterizeGamma);
#endif

#if SHINE_ON
	half2 uvShine = uvRect;
	half cosAngle = cos(_ShineRotate);
	half sinAngle = sin(_ShineRotate);
	half2x2 rot = half2x2(cosAngle, -sinAngle, sinAngle, cosAngle);
	uvShine -= half2(0.5, 0.5);
	uvShine = mul(rot, uvShine);
	uvShine += half2(0.5, 0.5);
	half shineMask = SAMPLE_TEXTURE2D(_ShineMask, sampler_ShineMask, i.uv).a;
	half currentDistanceProjection = (uvShine.x + uvShine.y) / 2;
	half whitePower = 1 - (abs(currentDistanceProjection - _ShineLocation) / _ShineWidth);
	col.rgb +=  col.a * whitePower * _ShineGlow * max(sign(currentDistanceProjection - (_ShineLocation - _ShineWidth)), 0.0)
	* max(sign((_ShineLocation + _ShineWidth) - currentDistanceProjection), 0.0) * _ShineColor * shineMask;
#endif

#if HOLOGRAM_ON
	half totalHologram = max(_HologramStripesAmount + _HologramUnmodAmount, 0.0001);
	half hologramYCoord = ((uvRect.y + (((_Time.x + randomSeed) % 1) * _HologramStripesSpeed)) % totalHologram) / totalHologram;
	hologramYCoord = abs(hologramYCoord);
	half alpha = RemapFloat(saturate(hologramYCoord - (_HologramUnmodAmount/totalHologram)), 0.0, 1.0, _HologramMinAlpha, saturate(_HologramMaxAlpha));
	half hologramMask = max(sign((_HologramUnmodAmount/totalHologram) - hologramYCoord), 0.0);
	half4 hologramResult = col;
	hologramResult.a *= lerp(alpha, 1, hologramMask);
	hologramResult.rgb *= max(1, _HologramMaxAlpha * max(sign(hologramYCoord - (_HologramUnmodAmount/totalHologram)), 0.0));
	hologramMask = 1 - step(0.01,hologramMask);
	hologramResult.rgb += hologramMask * _HologramStripeColor * col.a;
	col = lerp(col, hologramResult, _HologramBlend);
#endif

#if FLICKER_ON
	col.a *= saturate(col.a * step(frac(0.05 + (_Time.w + randomSeed) * _FlickerFreq), 1 - _FlickerPercent) + _FlickerAlpha);
#endif

col.a *= _Alpha;

#if ALPHACUTOFF_ON
	clip((1 - _AlphaCutoffValue) - (1 - col.a) - 0.01);
#endif

#if ALPHAROUND_ON
	col.a = step(_AlphaRoundThreshold, col.a);
#endif

#if ALPHAOUTLINE_ON
	half alphaOutlineRes = pow(1 - col.a, max(_AlphaOutlinePower, 0.0001)) * step(_AlphaOutlineMinAlpha, col.a) * _AlphaOutlineBlend;
	col.rgb = lerp(col.rgb, _AlphaOutlineColor.rgb * _AlphaOutlineGlow, alphaOutlineRes);
	col.a = lerp(col.a, 1, alphaOutlineRes > 1);
#endif

	col *= _Color;
	#if !defined(HDRP_PASS) && UNITY_VERSION >= 60000000
		col *= unity_SpriteColor;
	#endif

#if FOG_ON
	col = AllIn1MixFog(col, i.vertex, i.positionWS, i.fogCoord);
#endif

	return col;
}

#endif