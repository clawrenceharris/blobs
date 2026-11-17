using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public static class ShaderVariantCreator
	{
		private const string INCLUDE_LINE_FORMAT = @"#include ""{0}""";
		
		private const string TAG_VARIANT_NAME = @"<VARIANT_NAME>";
		private const string TAG_SHADER_PROPERTIES = @"<SHADER_PROPERTIES>";

		private const string TAG_SHADER_PASSES_URP	= @"<SHADER_PASSES_URP>";
		private const string TAG_SHADER_PASSES_BIRP = @"<SHADER_PASSES_BIRP>";

		//
		private const string TAG_PASS_NAME					= @"<PASS_NAME>";
		private const string TAG_LIGHT_MODE					= @"<LIGHT_MODE>";
		private const string TAG_BLEND_COMMAND				= @"<BLEND_COMMAND>";
		private const string TAG_CULL_COMMAND				= @"<CULL_COMMAND>";
		

		private const string TAG_Z_WRITE_COMMAND			= @"<Z_WRITE_COMMAND>";

		private const string TAG_Z_TEST_COMMAND				= @"<Z_TEST_COMMAND>";

		private const string TAG_COLOR_MASK_COMMAND			= @"<COLOR_MASK_COMMAND>";


		private const string TAG_STENCIL_BLOCK				= @"<STENCIL_BLOCK>";
		private const string TAG_FEATURES_URP_DEFINES		= @"<FEATURES_URP_DEFINES>";
		private const string TAG_FEATURES_URP_LIBRARY		= @"<FEATURES_URP_LIBRARY>";
		private const string TAG_VERTEX_PROGRAM_NAME		= @"<VERTEX_PROGRAM_NAME>";
		private const string TAG_FRAGMENT_PROGRAM_NAME		= @"<FRAGMENT_PROGRAM_NAME>";
		private const string TAG_ALL_IN_1_FEATURES			= @"<ALLIN1_FEATURES>";
		private const string TAG_PIPELINE_PASS_SYMBOL		= @"<PIPELINE_PASS_SYMBOL>";
		private const string TAG_THIS_PASS_SYMBOL			= @"<THIS_PASS_SYMBOL>";
		private const string TAG_INCLUDE_PIPELINE_HELPER	= @"<INCLUDE_PIPELINE_HELPER>";
		private const string TAG_INCLUDE_EFFECT_LIBRARIES	= @"<INCLUDE_EFFECT_LIBRARIES>";
		private const string TAG_INCLUDE_PASS				= @"<INCLUDE_PASS>";
		private const string TAG_EXTRA_PRAGMA_LINES			= @"<EXTRA_PRAGMA_LINES>";

		private const string TAG_INCLUDE_ALL_IN_SHADER_FEATURES = @"<INCLUDE_ALL_IN_1_SHADER_FEATURES>";
		private const string TAG_INCLUDE_ALL_IN_1_SHADER_LIGHT	= @"<INCLUDE_ALL_IN_1_SHADER_LIGHT>";
		private const string TAG_INCLUDE_CORE_LIBRARY			= @"<INCLUDE_CORE_LIBRARY>";
		private const string TAG_INCLUDE_COMMON_FUNCTIONS		= @"<INCLUDE_COMMON_FUNCTIONS>";
		private const string TAG_INCLUDE_COMMON_STRUCTS			= @"<INCLUDE_COMMON_STRUCTS>";
		//

		public const string SHADER_VARIANTS_FOLDER_NAME = "Baked Variants";
		private const string SHADER_VARIANT_FILE_NAME = "AllIn13D_BakedEffects_{0}.shader";
		
		public static Shader CreateVariantByMaterialInfo(Plugin3DConfig pluginConfig, PropertiesConfig propertiesConfig, AbstractMaterialInfo matInfo, string profileName)
		{
			EffectsProfileCollection effectsProfileCollection = pluginConfig.effectsProfileCollection;
			
			EffectsProfile effectsProfile = effectsProfileCollection.CreateNewProfile(profileName);

			effectsProfile.InitFromOtherProfile(effectsProfileCollection.generalProfile);
			effectsProfileCollection.ConfigureEffectProfileByMaterialInfo(
				target: effectsProfile, 
				activeEffectsList: effectsProfileCollection.generalProfile, 
				matInfo: matInfo, 
				propertiesConfig: propertiesConfig);

			string folderPath = pluginConfig.BakedShaderSavePath;
			Shader res = CreateVariant(pluginConfig, effectsProfile, effectsProfileCollection, folderPath, false);
			
			return res; 
		}

		public static Shader CreateVariant(Plugin3DConfig pluginConfig, EffectsProfileCollection effectsProfileCollection, string profileName)
		{
			EffectsProfile effectProfile = effectsProfileCollection.CreateNewProfile(profileName);
			effectProfile.InitFromOtherProfile(effectsProfileCollection.generalProfile);

			string folderPath = pluginConfig.BakedShaderSavePath;
			Shader res = CreateVariant(pluginConfig, effectProfile, effectsProfileCollection, folderPath, false);
			return res;
		}

		public static Shader CreateVariant(Plugin3DConfig pluginConfig, EffectsProfile effectsProfile, EffectsProfileCollection effectsProfileCollection, string folderPath, bool overwrite)
		{
			folderPath = EditorUtils.NormalizeAssetPath(folderPath);

			if (!overwrite && !EditorUtils.EnsureAssetFolderExists(folderPath))
			{
				Debug.LogError($"[AllIn1] Could not create the baked shaders folder '{folderPath}'. " +
					"Set a valid folder in the Save Paths tab of the plugin window.");
				return null;
			}

			string txtShaderVariant = File.ReadAllText(pluginConfig.ShaderTemplatePath);

			txtShaderVariant = ConfigureVariantName(txtShaderVariant, effectsProfile.profileName);
			txtShaderVariant = ConfigureShaderProperties(pluginConfig, txtShaderVariant);

			ShaderPassCollection shaderPassCollection = AssetDatabase.LoadAssetAtPath<ShaderPassCollection>(pluginConfig.ShaderPassCollectionPath);

			string urpPassesEntry = GetShaderPassesEntry(pluginConfig, effectsProfile, shaderPassCollection, RenderPipelineEnum.URP, folderPath);
			txtShaderVariant = txtShaderVariant.Replace(TAG_SHADER_PASSES_URP, urpPassesEntry);

			string birpPassesEntry = GetShaderPassesEntry(pluginConfig, effectsProfile, shaderPassCollection, RenderPipelineEnum.BIRP, folderPath);
			txtShaderVariant = txtShaderVariant.Replace(TAG_SHADER_PASSES_BIRP, birpPassesEntry);

			
			string filePath;
			if (overwrite)
			{
				Shader shader = effectsProfile.FindShader();
				filePath = AssetDatabase.GetAssetPath(shader);
			}
			else
			{
				string fileName = string.Format(SHADER_VARIANT_FILE_NAME, effectsProfile.profileName);
				filePath = EditorUtils.CombineAssetPath(folderPath, fileName);

				filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);
			}

			if (string.IsNullOrEmpty(filePath))
			{
				Debug.LogError("[AllIn1] Could not resolve a path for the baked shader, nothing was written.");
				return null;
			}

			effectsProfile.shaderGUID = AssetDatabase.AssetPathToGUID(filePath);


			txtShaderVariant = EditorUtils.UnifyEOL(txtShaderVariant);
			SaveFile(filePath, txtShaderVariant);
			AssetDatabase.Refresh();

			Shader res = AssetDatabase.LoadAssetAtPath(filePath, typeof(Shader)) as Shader;
			EditorUtility.SetDirty(effectsProfileCollection);

			return res;
		}

		private static void SaveFile(string filePath, string txtFile)
		{
			EditorUtils.EnsureAssetFolderExists(EditorUtils.GetAssetParentFolder(filePath));

			File.WriteAllText(filePath, txtFile);
		}

		private static string ConfigureVariantName(string input, string variantName)
		{
			string res = input.Replace(TAG_VARIANT_NAME, variantName);
			return res;
		}

		private static string ConfigureShaderProperties(Plugin3DConfig pluginConfig, string input)
		{
			string res = input;

			string shaderPropertiesTxt = EditorUtils.ReadFileTextWithTabs(pluginConfig.ShaderPropertiesPath, 2);
			res = res.Replace(TAG_SHADER_PROPERTIES, shaderPropertiesTxt);

			return res;
		}

		private static string GetShaderPassesEntry(Plugin3DConfig pluginConfig, EffectsProfile effectsProfile, ShaderPassCollection shaderPassCollection, RenderPipelineEnum renderPipeline, string shaderFolderPath)
		{
			string shaderPassTemplateTxt = EditorUtils.ReadFileTextWithTabs(GetTemplatePathByRenderPipeline(pluginConfig, renderPipeline), 2);

			List<ShaderPassConfig> shaderPasses = effectsProfile.GetShaderPasses(shaderPassCollection, renderPipeline);

			bool hasStencilBlock = false;
			for(int i = 0; i < shaderPasses.Count; i++)
			{
				if (shaderPasses[i].passType == AllIn13DPassType.OUTLINE)
				{
					hasStencilBlock = true;
					break;
				}
			}

			string res = string.Empty;
			for (int i = 0; i < shaderPasses.Count; i++)
			{
				string currentPass = shaderPassTemplateTxt;
				currentPass = ConfigureShaderPass(effectsProfile, currentPass, shaderPasses[i], renderPipeline, hasStencilBlock, shaderFolderPath, pluginConfig);

				res += currentPass;
				res += "\n";
			}

			return res;
		}

		private static string ConfigureShaderPass(EffectsProfile effectsProfile, 
			string templatePass, ShaderPassConfig shaderPass, RenderPipelineEnum renderPipeline, bool hasStencilBlock, 
			string shaderFolderPath, Plugin3DConfig pluginConfig)
		{
			string res = templatePass;

			res = res.Replace(TAG_PASS_NAME, shaderPass.GetPassName(renderPipeline));

			res = res.Replace(TAG_LIGHT_MODE, shaderPass.GetLightModeShaderEntry(renderPipeline));

			res = res.Replace(TAG_BLEND_COMMAND, shaderPass.blendCommand.GetShaderEntry());
			
			res = res.Replace(TAG_CULL_COMMAND, shaderPass.cullCommand.GetShaderEntry());
			res = res.Replace(TAG_Z_WRITE_COMMAND, shaderPass.zWriteCommand.GetShaderEntry());
			res = res.Replace(TAG_Z_TEST_COMMAND, shaderPass.zTestCommand.GetShaderEntry());
			res = res.Replace(TAG_COLOR_MASK_COMMAND, shaderPass.colorMaskCommand.GetShaderEntry());

			if (hasStencilBlock)
			{
				res = res.Replace(TAG_STENCIL_BLOCK, shaderPass.GetStencilBlockShaderEntry(effectsProfile));
			}
			else
			{
				res = res.Replace(TAG_STENCIL_BLOCK, string.Empty);
			}

			res = res.Replace(TAG_FEATURES_URP_DEFINES, shaderPass.GetPipelineFeaturesDefinesShaderEntry(renderPipeline, shaderFolderPath, pluginConfig));
			res = res.Replace(TAG_FEATURES_URP_LIBRARY, shaderPass.GetPipelineFeaturesLibraryShaderEntry(renderPipeline, shaderFolderPath, pluginConfig));

			res = res.Replace(TAG_VERTEX_PROGRAM_NAME, shaderPass.vertexProgramName);
			res = res.Replace(TAG_FRAGMENT_PROGRAM_NAME, shaderPass.fragmentProgramName);

			res = res.Replace(TAG_INCLUDE_PIPELINE_HELPER, string.Format(INCLUDE_LINE_FORMAT, shaderPass.GetHelperLibraryPath(renderPipeline, shaderFolderPath, pluginConfig)));

			string effectsLibraries = shaderPass.GetEffectsLibrariesShaderEntry(effectsProfile, shaderFolderPath, pluginConfig);
			res = res.Replace(TAG_INCLUDE_EFFECT_LIBRARIES, effectsLibraries);

			res = res.Replace(TAG_INCLUDE_PASS, string.Format(INCLUDE_LINE_FORMAT, shaderPass.GetPassHLSLFilePath(shaderFolderPath, pluginConfig)));

			List<EffectsProfileEntry> enabledEntries = effectsProfile.GetEnabledEntriesFlatList();
			string entryFeaturesTxt = ShaderPassConfig.GetAllIn1FeaturesEntries(enabledEntries);
			
			res = res.Replace(TAG_ALL_IN_1_FEATURES, entryFeaturesTxt);

			res = res.Replace(TAG_PIPELINE_PASS_SYMBOL, shaderPass.GetPipelinePassSymbolShaderEntry(renderPipeline));
			res = res.Replace(TAG_THIS_PASS_SYMBOL, shaderPass.GetPassSymbolShaderEntry());

			res = res.Replace(TAG_EXTRA_PRAGMA_LINES, shaderPass.GetExtraPragmaLines(renderPipeline));

			res = res.Replace(TAG_INCLUDE_ALL_IN_SHADER_FEATURES, shaderPass.GetShaderFeaturesLibraryShaderEntry(shaderFolderPath, pluginConfig));
			res = res.Replace(TAG_INCLUDE_ALL_IN_1_SHADER_LIGHT, shaderPass.GetLightLibraryShaderEntry(shaderFolderPath, pluginConfig));
			res = res.Replace(TAG_INCLUDE_CORE_LIBRARY, shaderPass.GetCoreLibraryShaderEntry(shaderFolderPath, pluginConfig));
			res = res.Replace(TAG_INCLUDE_COMMON_STRUCTS, shaderPass.GetCommonStructsShaderEntry(shaderFolderPath, pluginConfig));
			res = res.Replace(TAG_INCLUDE_COMMON_FUNCTIONS, shaderPass.GetCommonFunctionsShaderEntry(shaderFolderPath, pluginConfig));

			return res;
		}

		private static string GetTemplatePathByRenderPipeline(Plugin3DConfig pluginConfig, RenderPipelineEnum renderPipeline)
		{
			string res = string.Empty;
			switch (renderPipeline)
			{
				case RenderPipelineEnum.BIRP:
					res = pluginConfig.BIRPTemplatePath;
					break;

				case RenderPipelineEnum.URP:
					res = pluginConfig.URPTemplatePath;
					break;
			}

			return res;
		}

		public static string GetShaderVariantsFolderPath(Plugin3DConfig pluginConfig)
		{
			string res = pluginConfig.BakedShaderSavePath;
			return res;
		}
	}
}