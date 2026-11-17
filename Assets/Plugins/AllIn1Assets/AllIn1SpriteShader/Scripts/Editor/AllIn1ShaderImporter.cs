#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.AllIn1SpriteShader
{
	[InitializeOnLoad]
	public static class AllIn1ShaderImporter
	{
		public enum UnityVersion
		{
			NONE = 0,
			UNITY_2022 = 4,
			UNITY_6 = 5,
		}

		public enum RenderPipeline
		{
			NONE = -1,
			BIRP = 0,
			URP = 1,
			HDRP = 2,
		}


		private const string SHADER_FOLDER_PATH = "../../Shaders/LitShaders";
		private const string FINAL_SHADERS_FOLDER_PATH = "../../Shaders";
		private const string SHADER_TEMPLATE_NAME = @"{0}_{1}.txt";

		private const string SPRITE_LIT_SHADER_NAME = "AllIn1SpriteShaderLit";
		private const string SPRITE_LIT_TRANSPARENT_SHADER_NAME = "AllIn1SpriteShaderLitTransparent";

		private const string TEMPLATE_STANDARD = "BetterShader_Standard";
		private const string TEMPLATE_PIPELINE_FORMAT = "BetterShader_{0}{1}";
		private const string TEMPLATE_NEWEST_VERSION = "2023";

		private const string LIT_SHADER_PIPELINE_KEY = "AllIn1SpriteShader_LitShader_RenderPipeline";
		private const string LIT_SHADER_UNITY_VERSION_KEY = "AllIn1SpriteShader_LitShader_UnityVersion";
		private const string LIT_SHADER_FIRST_TIME_PROJECT = "AllIn1SpriteShader_LitShader_FirstTimeProject";

		//True while the importer imports a shader it just wrote, so the postprocessor ignores that import
		public static bool IsWritingLitShader { get; private set; }

		static AllIn1ShaderImporter()
		{
			ConfigureShaders();
		}

		private static void ConfigureShaders()
		{
			if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Secondary)
				return;

			RenderPipelineChecker.RefreshData();

			UnityVersion unityVersion = GetUnityVersion();
			RenderPipeline renderPipeline = GetRenderPipeline();

			RenderPipeline lastRenderPipeline = (RenderPipeline)SessionState.GetInt(LIT_SHADER_PIPELINE_KEY, -1);
			UnityVersion lastUnityVersion = (UnityVersion)SessionState.GetInt(LIT_SHADER_UNITY_VERSION_KEY, 0);
			int firstTimeProject = SessionState.GetInt(LIT_SHADER_FIRST_TIME_PROJECT, -1);

			if (lastRenderPipeline != renderPipeline || lastUnityVersion != unityVersion || firstTimeProject != 1)
			{
				SessionState.SetInt(LIT_SHADER_PIPELINE_KEY, (int)renderPipeline);
				SessionState.SetInt(LIT_SHADER_UNITY_VERSION_KEY, (int)unityVersion);
				SessionState.SetInt(LIT_SHADER_FIRST_TIME_PROJECT, 1);

				ConfigureShader(SPRITE_LIT_SHADER_NAME);
				ConfigureShader(SPRITE_LIT_TRANSPARENT_SHADER_NAME);
			}
		}

		private static void ConfigureShader(string shaderName)
		{
			string pipelineSufix = GetTemplateSuffix(GetRenderPipeline(), GetUnityVersion());

			string templateFileName = string.Format(SHADER_TEMPLATE_NAME, shaderName, pipelineSufix);

			string shaderTemplatePath = SHADER_FOLDER_PATH + "/" + templateFileName;
			string finalShaderPath = FINAL_SHADERS_FOLDER_PATH + "/" + $"{shaderName}.shader";

			try
			{
				string[] results = AssetDatabase.FindAssets($"t:Script {nameof(AllIn1ShaderImporter)}");
				if (results.Length == 0)
				{
					Debug.LogError($"Could not locate {nameof(AllIn1ShaderImporter)} in the project, {shaderName} was not configured");
					return;
				}

				string currentFolder = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(results[0]));

				string sourceFilePath = Path.GetFullPath(Path.Combine(currentFolder, shaderTemplatePath));
				string targetFilePath = Path.GetFullPath(Path.Combine(currentFolder, finalShaderPath));
				string targetAssetPath = ToAssetPath(targetFilePath);

				string newShaderStr = File.ReadAllText(sourceFilePath);
				newShaderStr = newShaderStr.Replace($"Shader \"AllIn1SpriteShader/{shaderName}_BetterShader\"", $"Shader \"AllIn1SpriteShader/{shaderName}\"");
				//Unity's shader upgrader makes this rewrite on import, so without it the shader never equals its template again
				newShaderStr = newShaderStr.Replace("defined FOG_COMBINED_WITH_WORLD_POS", "defined (FOG_COMBINED_WITH_WORLD_POS)");
				newShaderStr = EditorUtils.UnifyEOL(newShaderStr);

				if (File.Exists(targetFilePath))
				{
					//Normalize the existing file too, its line endings depend on how it was checked out (git autocrlf and similar)
					string existingShaderStr = EditorUtils.UnifyEOL(File.ReadAllText(targetFilePath));
					if (string.Equals(existingShaderStr, newShaderStr, StringComparison.Ordinal))
						return;
				}

				File.WriteAllText(targetFilePath, newShaderStr);

				IsWritingLitShader = true;
				try
				{
					AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);
				}
				finally
				{
					IsWritingLitShader = false;
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"{shaderName} could not be generated from template {templateFileName}: {e}");
			}
		}

		private static string ToAssetPath(string absolutePath)
		{
			string fullPath = absolutePath.Replace('\\', '/');
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');

			if (!projectRoot.EndsWith("/", StringComparison.Ordinal))
				projectRoot += "/";

			return fullPath.StartsWith(projectRoot, StringComparison.Ordinal)
				? fullPath.Substring(projectRoot.Length)
				: fullPath;
		}

		private static string GetTemplateSuffix(RenderPipeline renderPipeline, UnityVersion unityVersion)
		{
			string pipelineName = GetPipelineTemplateName(renderPipeline);
			if (string.IsNullOrEmpty(pipelineName))
				return TEMPLATE_STANDARD;

			return string.Format(TEMPLATE_PIPELINE_FORMAT, pipelineName, GetUnityVersionTemplateName(unityVersion));
		}

		private static string GetPipelineTemplateName(RenderPipeline renderPipeline)
		{
			switch (renderPipeline)
			{
				case RenderPipeline.URP:
					return "URP";
				case RenderPipeline.HDRP:
					return "HDRP";
				default:
					return string.Empty;
			}
		}

		//Unrecognised versions fall back to the newest template instead of to no template at all
		private static string GetUnityVersionTemplateName(UnityVersion unityVersion)
		{
			switch (unityVersion)
			{
				case UnityVersion.UNITY_2022:
					return "2022";
				default:
					return TEMPLATE_NEWEST_VERSION;
			}
		}

		private static UnityVersion GetUnityVersion()
		{
			string unityVersion = Application.unityVersion;

			if (unityVersion.StartsWith("2022", StringComparison.Ordinal))
				return UnityVersion.UNITY_2022;
			if (unityVersion.StartsWith("6000", StringComparison.Ordinal))
				return UnityVersion.UNITY_6;

			return UnityVersion.NONE;
		}

		private static RenderPipeline GetRenderPipeline()
		{
			RenderPipeline res = RenderPipeline.BIRP;

			if (RenderPipelineChecker.IsURP())
			{
				res = RenderPipeline.URP;
			}
			else if (RenderPipelineChecker.IsHDRP())
			{
				res = RenderPipeline.HDRP;
			}

			return res;
		}

		public static void ForceReimport()
		{
			InvalidateSession();
			ConfigureShaders();
		}

		public static void InvalidateSession()
		{
			SessionState.EraseInt(LIT_SHADER_PIPELINE_KEY);
			SessionState.EraseInt(LIT_SHADER_UNITY_VERSION_KEY);
			SessionState.EraseInt(LIT_SHADER_FIRST_TIME_PROJECT);
		}

		//A package update writes the shipped Built-in shaders over the generated ones and may change the templates
		public static bool IsLitShaderFile(string assetPath)
		{
			string fileName = Path.GetFileName(assetPath);
			if (fileName.EndsWith(".txt", StringComparison.Ordinal) &&
			    (fileName.StartsWith(SPRITE_LIT_SHADER_NAME + "_BetterShader_", StringComparison.Ordinal) ||
			     fileName.StartsWith(SPRITE_LIT_TRANSPARENT_SHADER_NAME + "_BetterShader_", StringComparison.Ordinal)))
				return true;

			if (!string.Equals(fileName, SPRITE_LIT_SHADER_NAME + ".shader", StringComparison.Ordinal) &&
			    !string.Equals(fileName, SPRITE_LIT_TRANSPARENT_SHADER_NAME + ".shader", StringComparison.Ordinal))
				return false;

			//On Built-in the shipped shaders are already the right ones
			RenderPipelineChecker.RefreshData();
			return GetRenderPipeline() != RenderPipeline.BIRP;
		}
	}
}
#endif