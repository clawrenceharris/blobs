using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class ShaderPassConfig : ScriptableObject
	{
		private const string STENCIL_BLOCK_OUTLINE = @"
			Stencil
			{
				Ref [_StencilRef]
				Comp [_OutlineMode]
			}";

		private const string STENCIL_BLOCK_MAIN_PASS = @"
			Stencil
            {
                 Ref [_StencilRef]
                 Comp Always
				 Pass Replace
			}";

		private const string URP_PASS_SYMBOL = "URP_PASS";
		private const string BIRP_PASS_SYMBOL = "BIRP_PASS";


		private const string EFFECT_LIBRARY_PATH_FORMAT = @"Shaders/ShaderLibrary/{0}.hlsl";
		private const string INCLUDE_SHADER_ENTRY_FORMAT = @"#include ""{0}""";
		private const string INCLUDE_WITH_PRAGMA_SHADER_ENTRY_FORMAT = @"#include_with_pragmas ""{0}""";
		private const string DEFINE_SHADER_ENTRY_FORMAT = @"#define {0}";
		private const string LIGHT_MODE_ENTRY_FORMAT = @"""LightMode""=""{0}""";

		public AllIn13DPassType passType;

		[Header("Pass Name")]
		public string passNameURP;
		public string passNameBIRP;

		[Header("Light Mode")]
		public string lightModeTagURP;
		public string lightModeTagBIRP;

		[Header("Pass Symbol")]
		public string passSymbol;
		
		[Header("Vertex/Fragment Program")]
		public string vertexProgramName;
		public string fragmentProgramName;

		[Header("Command")]
		public BlendCommand blendCommand;
		public CullCommand cullCommand;
		public ZWriteCommand zWriteCommand;
		public ZTestCommand zTestCommand;
		public ColorMaskCommand colorMaskCommand;

		[Header("Extra Pragma - BIRP")]
		[TextArea]public string extraPragmaLinesBIRP;

		[Header("Extra Pragma - URP")]
		[TextArea] public string extraPragmaLinesURP;

		public string GetHelperLibraryPath(RenderPipelineEnum renderPipeline, string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string res;
			switch (renderPipeline)
			{
				case RenderPipelineEnum.BIRP:
					res = pluginConfig.BIRPHelperPath;
					break;
				case RenderPipelineEnum.URP:
					res = pluginConfig.URPHelperPath;
					break;
				default:
					res = string.Empty;
					break;
			}

			res = ConvertPathToRelative(res, false, shaderFolder, pluginConfig);

			return res;
		}

		private string GetPipelineFeatuersLibrary(RenderPipelineEnum renderPipeline, string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string res = string.Empty;

			if (renderPipeline == RenderPipelineEnum.URP)
			{
				res = "Shaders/ShaderLibrary/AllIn13DShader_FeaturesURP.hlsl";
			}

			res = ConvertPathToRelative(res, true, shaderFolder, pluginConfig);

			return res;
		}

		private string GetPipelineFeaturesDefinesPath(RenderPipelineEnum renderPipeline, string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string res = string.Empty;

			if (renderPipeline == RenderPipelineEnum.URP)
			{
				res = "Shaders/ShaderLibrary/AllIn13DShader_FeaturesURP_Defines.hlsl";
			}

			res = ConvertPathToRelative(res, true, shaderFolder, pluginConfig);

			return res;
		}

		private string GetPipelinePassSymbol(RenderPipelineEnum renderPipeline)
		{
			string res = string.Empty;

			switch (renderPipeline)
			{
				case RenderPipelineEnum.BIRP:
					res = BIRP_PASS_SYMBOL;
					break;
				case RenderPipelineEnum.URP:
					res = URP_PASS_SYMBOL;
					break;
			}

			return res;
		}

		public string GetStencilBlockShaderEntry(EffectsProfile effectsProfile)
		{
			string res = string.Empty;


			if (passType == AllIn13DPassType.OUTLINE)
			{
				res = STENCIL_BLOCK_OUTLINE;
			}
			else if(passType == AllIn13DPassType.MAIN)
			{
				res = STENCIL_BLOCK_MAIN_PASS;
			}

			return res;
		}

		public string GetPipelineFeaturesDefinesShaderEntry(RenderPipelineEnum renderPipeline, string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string definesFilePath = GetPipelineFeaturesDefinesPath(renderPipeline, shaderFolder, pluginConfig);

			string res = CreateIncludeShaderEntry(definesFilePath);
			return res;
		}

		public string GetPipelineFeaturesLibraryShaderEntry(RenderPipelineEnum renderPipeline, string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string libraryPath = GetPipelineFeatuersLibrary(renderPipeline, shaderFolder, pluginConfig);
			string res = GetIncludeWithPragmaShaderEntry(libraryPath);

			return res;
		}

		public string GetPipelinePassSymbolShaderEntry(RenderPipelineEnum renderPipeline)
		{
			string pipelineSymbol = GetPipelinePassSymbol(renderPipeline);
			string res = string.Format(DEFINE_SHADER_ENTRY_FORMAT, pipelineSymbol);

			return res;
		}

		public string GetIncludeWithPragmaShaderEntry(string path)
		{
			string res = string.Format(INCLUDE_WITH_PRAGMA_SHADER_ENTRY_FORMAT, path);
			return res;
		}

		public string CreateIncludeShaderEntry(string value)
		{
			string res = string.Format(INCLUDE_SHADER_ENTRY_FORMAT, value);
			return res;
		}

		public string GetPassSymbolShaderEntry()
		{
			string res = string.Format(DEFINE_SHADER_ENTRY_FORMAT, passSymbol);
			return res;
		}

		public string GetLightLibraryShaderEntry(string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string res = ConvertPathToRelative("Shaders/ShaderLibrary/AllIn13DShaderLight.hlsl", true, shaderFolder, pluginConfig);
			res = CreateIncludeShaderEntry(res);

			return res;
		}

		public string GetCoreLibraryShaderEntry(string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string res = ConvertPathToRelative("Shaders/ShaderLibrary/AllIn13DShaderCore.hlsl", true, shaderFolder, pluginConfig);
			res = CreateIncludeShaderEntry(res);

			return res;
		}

		public string GetCommonStructsShaderEntry(string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string res = ConvertPathToRelative("Shaders/ShaderLibrary/AllIn13DShader_CommonStructs.hlsl", true, shaderFolder, pluginConfig);
			res = CreateIncludeShaderEntry(res);

			return res;
		}

		public string GetCommonFunctionsShaderEntry(string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string res = ConvertPathToRelative("Shaders/ShaderLibrary/AllIn13DShader_CommonFunctions.hlsl", true, shaderFolder, pluginConfig);
			res = CreateIncludeShaderEntry(res);

			return res;
		}

		public string GetShaderFeaturesLibraryShaderEntry(string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string res = ConvertPathToRelative("Shaders/ShaderLibrary/AllIn13DShader_Features.hlsl", true, shaderFolder, pluginConfig);
			res = GetIncludeWithPragmaShaderEntry(res);

			return res;
		}

		public string GetLightModeShaderEntry(RenderPipelineEnum renderPipeline)
		{
			string res = string.Empty;
			string lightTag = string.Empty;
			if(renderPipeline == RenderPipelineEnum.BIRP)
			{
				lightTag = lightModeTagBIRP;
			}
			else if(renderPipeline == RenderPipelineEnum.URP)
			{
				lightTag = lightModeTagURP;
			}

			if (!string.IsNullOrEmpty(lightTag))
			{
				res = string.Format(LIGHT_MODE_ENTRY_FORMAT, lightTag);
			}

			return res;
		}

		public static string GetAllIn1FeaturesEntries(List<EffectsProfileEntry> enabledEntries)
		{
			string res = string.Empty;
			for (int i = 0; i < enabledEntries.Count; i++)
			{
				res = AppendEntryKeywords(res, enabledEntries[i]);
			}

			return res;
		}

		public static string AppendEntryKeywords(string input, EffectsProfileEntry effectProfileEntry)
		{
			string res = input;

			List<string> keywordsEnabled = new List<string>();
			effectProfileEntry.CollectKeywords(keywordsEnabled);

			for (int i = 0; i < keywordsEnabled.Count; i++)
			{
				res += CreateDefineEntry(keywordsEnabled[i]);
				res += "\n";
			}

			return res;
		}


		private string ConvertPathToRelative(string path, bool isLocalPath, string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string res = path;
			if (isLocalPath)
			{
				res = Path.Combine(pluginConfig.RootPluginPath, path);
			}

			res = Path.GetRelativePath(shaderFolder, res);

			res = res.Replace("\\", "/");

			return res;
		}

		public string GetPassHLSLFilePath(string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string res = pluginConfig.GetPassHLSLFilePath(passType);
			res = ConvertPathToRelative(res, false, shaderFolder, pluginConfig);

			return res;
		}

		public static string CreateDefineEntry(string symbol)
		{
			string res = string.Format(DEFINE_SHADER_ENTRY_FORMAT, symbol);
			return res;
		}

		public string GetPassName(RenderPipelineEnum renderPipeline)
		{
			string res = passNameBIRP;
			if(renderPipeline == RenderPipelineEnum.URP)
			{
				res = passNameURP;
			}

			return res;
		}

		public string GetEffectsLibrariesShaderEntry(EffectsProfile effectsProfile, string shaderFolder, Plugin3DConfig pluginConfig)
		{
			string res = string.Empty;

			List<EffectsProfileGroup> enabledEffectGroups = effectsProfile.GetEnabledEffectsGroups();

			for(int i = 0; i < enabledEffectGroups.Count; i++)
			{
				string libraryFileName = enabledEffectGroups[i].effectGroupConfig.libraryFileName;

				if (!string.IsNullOrEmpty(libraryFileName))
				{
					string path = string.Format(EFFECT_LIBRARY_PATH_FORMAT, libraryFileName);
					path = ConvertPathToRelative(path, true, shaderFolder, pluginConfig);

					string entry = CreateIncludeShaderEntry(path);

					res += entry;
					res += "\n";
				}
			}

			return res;
		}

		public string GetExtraPragmaLines(RenderPipelineEnum renderPipeline)
		{
			string res = string.Empty;

			if(renderPipeline == RenderPipelineEnum.BIRP)
			{
				res = extraPragmaLinesBIRP;
			}
			else if(renderPipeline == RenderPipelineEnum.URP)
			{
				res = extraPragmaLinesURP;
			}

			return res;
		}
	}
}