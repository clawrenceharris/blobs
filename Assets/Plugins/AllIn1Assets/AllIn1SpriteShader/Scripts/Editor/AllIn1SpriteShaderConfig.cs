#if ALLIN1_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

using AllIn1Assets.SharedExtensions;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.AllIn1SpriteShader
{
	public class AllIn1SpriteShaderConfig : Plugin3DConfig
	{
		private const string PLUGIN_NAME = "AllIn1SpriteShader";
		private const string VERSION = "4.71";
		private const PluginID PLUGIN_ID = PluginID.ALLIN1_SPRITE;

		private static string[] SHADER_NAMES = 
		{ 
			"AllIn1SpriteShader",
			"AllIn1SpriteShaderLit",
			"AllIn1SpriteShaderLitTransparent",
			"AllIn1SpriteShaderScaledTime",
			"AllIn1SpriteShaderSRPBatch",
			"AllIn1SpriteShaderUiMask",
			"AllIn1SpriteShaderUiMaskScaledTime",
			"AllIn1Urp2dRenderer"
		};

		//The Others tab of the plugin window writes this, and it decides the variant of every material
		//the asset creates from scratch. Static and read from prefs: it must answer even when the
		//registry has no config yet, which is the state right after an import
		public static AllIn1Shader.ShaderTypes DefaultShaderType
		{
			get
			{
				return (AllIn1Shader.ShaderTypes)PlayerPrefs.GetInt(AllIn1Shader.PREF_DEFAULT_SHADER,
					(int)AllIn1Shader.ShaderTypes.Default);
			}
			set
			{
				PlayerPrefs.SetInt(AllIn1Shader.PREF_DEFAULT_SHADER, (int)value);
				PlayerPrefs.Save();
			}
		}

		//Invalid means the component has no AllIn1 material yet, so the window default decides
		public static string ResolveShaderName(AllIn1Shader.ShaderTypes shaderType)
		{
			if (shaderType == AllIn1Shader.ShaderTypes.Invalid)
			{
				return AllIn1Shader.GetShaderName(DefaultShaderType);
			}

			return AllIn1Shader.GetShaderName(shaderType);
		}

		public override PluginID GetPluginID()
		{
			return PLUGIN_ID;
		}

		public override string GetPluginName()
		{
			return PLUGIN_NAME;
		}

		public override Shader GetShaderAfterMaterialRemoval()
		{
			Shader res = Shader.Find("Sprites/Default");
			return res;
		}

		public override string[] GetShaderNames()
		{
			return SHADER_NAMES;
		}

		public override string[] GetShaderTemplates()
		{
			return null;
		}

		public override string GetVersion()
		{
			return VERSION;
		}

		public override bool IsDerivedFromAllIn13DShader()
		{
			return false;
		}

		public override bool IsWritableFolder(string folderPath)
		{
			return false;
		}

		public override string GetRenderPipelineDocURL()
		{
			return $"{GlobalConstants.URL_DOCS_SITE}/sprite-shader/render-pipeline-and-post-processing-setup";
		}


#if ALLIN1_DEVELOP
		public override string[] GetFoldersToExport()
		{
			string[] res = base.GetFoldersToExport();
			ArrayUtility.Add(ref res, Shared3DRootFolderPath);

			return res;
		}
#endif


#if ALLIN1_URP
		public override void ConvertMaterialsFolder() 
		{

		}

		public override void ConfigureRenderPipeline(UniversalRendererData universalRendererData, RenderPipelineAsset renderPipelineAsset)
		{

		}
#endif
	}
}