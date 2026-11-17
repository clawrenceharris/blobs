using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class NoiseCreatorTool
	{
		private const string SHADER_NAME_FRACTAL_NOISE = "AllIn13DShaderFractalNoise";
		private const string SHADER_NAME_WORLEY_NOISE = "AllIn13DShaderWorleyNoise";

		public NoiseCreatorValues values;

		public bool HasMaterial => values.noiseMaterial != null;

		public NoiseCreatorTool()
		{
			values = new NoiseCreatorValues();

			NoiseSetMaterial();

			//The noise shaders are not in the asset database yet on a fresh import. The drawer calls
			//NoiseSetMaterial again on the next edit, so the tool just starts without a preview
			if (!HasMaterial) return;

			CheckCreationNoiseTextures();
			UpdateNoiseMatAndRender();
			CreateNoiseTex();
		}

		public void CreateNoiseTex()
		{
			if (!HasMaterial) return;

			int texSize = (int)values.noiseSize;
			values.finalNoiseTex = new Texture2D(texSize, texSize);
			RenderTexture finalRenderTarget = new RenderTexture(values.finalNoiseTex.width, values.finalNoiseTex.height, 0, RenderTextureFormat.ARGB32);
			Graphics.Blit(values.finalNoiseTex, finalRenderTarget, values.noiseMaterial);
			values.finalNoiseTex.ReadPixels(new Rect(0, 0, finalRenderTarget.width, finalRenderTarget.height), 0, 0);
			values.finalNoiseTex.Apply();
		}

		public void CheckCreationNoiseTextures()
		{
			if (values.noisePreview == null)
			{
				values.noisePreview = new Texture2D(256, 256);
			}

			if (values.noiseRenderTarget == null)
			{
				values.noiseRenderTarget = new RenderTexture(values.noisePreview.width, values.noisePreview.height, 0, RenderTextureFormat.ARGB32);
			}
		}

		public void UpdateNoiseMatAndRender()
		{
			if (!HasMaterial) return;

			if (values.noiseType == NoiseTypes.Fractal || values.noiseType == NoiseTypes.Perlin || values.noiseType == NoiseTypes.Billow)
			{
				values.noiseMaterial.SetFloat("_EndBand", values.noiseFractalAmount);
			}
			else values.noiseMaterial.SetFloat("_Jitter", values.noiseJitter);

			values.noiseMaterial.SetFloat("_ScaleX", values.noiseScaleX);
			values.noiseMaterial.SetFloat("_ScaleY", values.noiseScaleY);
			values.noiseMaterial.SetFloat("_Offset", (float)values.noiseSeed);
			values.noiseMaterial.SetFloat("_Contrast", values.noiseContrast);
			values.noiseMaterial.SetFloat("_Brightness", values.noiseBrightness);
			values.noiseMaterial.SetFloat("_Invert", values.noiseInverted ? 1f : 0f);

			Graphics.Blit(values.noisePreview, values.noiseRenderTarget, values.noiseMaterial);
			values.noisePreview.ReadPixels(new Rect(0, 0, values.noiseRenderTarget.width, values.noiseRenderTarget.height), 0, 0);
			values.noisePreview.Apply();
		}

		public void NoiseSetMaterial()
		{
			string shaderName = SHADER_NAME_WORLEY_NOISE;

			if (values.noiseType == NoiseTypes.Fractal || values.noiseType == NoiseTypes.Perlin || values.noiseType == NoiseTypes.Billow)
			{
				values.isFractalNoise = true;
				values.noiseScaleX = 4f;
				values.noiseScaleY = 4f;

				shaderName = SHADER_NAME_FRACTAL_NOISE;
			}
			else
			{
				values.isFractalNoise = false;
				values.noiseScaleX = 10f;
				values.noiseScaleY = 10f;
			}

			Shader shader = EditorUtils.FindShader(shaderName);

			//new Material(null) throws, and the shader search comes back empty while the package is importing
			values.noiseMaterial = shader != null ? new Material(shader) : null;

			if (!HasMaterial) return;

			switch (values.noiseType)
			{
				case NoiseTypes.Fractal:
					values.noiseFractalAmount = 8f;
					values.noiseMaterial.SetFloat("_Fractal", 1);
					break;
				case NoiseTypes.Perlin:
					values.noiseFractalAmount = 1f;
					values.noiseMaterial.SetFloat("_Fractal", 1);
					break;
				case NoiseTypes.Billow:
					values.noiseFractalAmount = 4f;
					values.noiseMaterial.SetFloat("_Fractal", 0);
					break;
				case NoiseTypes.Voronoi:
					values.noiseMaterial.SetFloat("_NoiseType", 0f);
					break;
				case NoiseTypes.Water:
					values.noiseMaterial.SetFloat("_NoiseType", 3f);
					break;
				case NoiseTypes.Cellular:
					values.noiseMaterial.SetFloat("_NoiseType", 4f);
					break;
				case NoiseTypes.Cells1:
					values.noiseMaterial.SetFloat("_NoiseType", 1f);
					break;
				case NoiseTypes.Cells2:
					values.noiseMaterial.SetFloat("_NoiseType", 2f);
					break;
			}
		}
	}
}