using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class RenderMaterialToImageTool
	{
		public static void RenderAndSaveTexture(Material targetMaterial, Texture targetTexture, float scaleSlider, string folderPath, string fileName, string pluginName)
		{
			RenderTexture renderTarget = new RenderTexture((int)(targetTexture.width * scaleSlider),
				(int)(targetTexture.height * scaleSlider), 0, RenderTextureFormat.ARGB32);
			Graphics.Blit(targetTexture, renderTarget, targetMaterial);
			Texture2D resultTex = new Texture2D(renderTarget.width, renderTarget.height, TextureFormat.ARGB32, false);
			resultTex.ReadPixels(new Rect(0, 0, renderTarget.width, renderTarget.height), 0, 0);
			resultTex.Apply();
			
			if (!Directory.Exists(folderPath))
			{
				EditorUtility.DisplayDialog("The desired Material to Image Save Path doesn't exist",
					$"Go to Tools -> AllIn1 -> {pluginName} and set a valid folder", "Ok");
				return;
			}

			string fullPath = EditorUtils.CombineAssetPath(folderPath, fileName + ".png");
			fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

			string correctedFileName = fullPath.Replace(folderPath, string.Empty);
			
			fullPath = EditorUtility.SaveFilePanel("Save Render Image", folderPath, fileName, "png");
			if (string.IsNullOrEmpty(fullPath))
			{
				GameObject.DestroyImmediate(resultTex);
				return;
			}

			byte[] bytes = resultTex.EncodeToPNG();
			File.WriteAllBytes(fullPath, bytes);

			GameObject.DestroyImmediate(resultTex);

			AssetDatabase.Refresh();
			EditorUtils.PingPath(fullPath);

			EditorUtils.ShowNotification("Render Image saved to: " + fullPath + " with scale: " + scaleSlider +
			" (it can be changed in Tools -> AllIn1 -> 3DShaderWindow)");
		}
	}
}