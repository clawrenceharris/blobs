using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public abstract class Plugin3DConfig : AbstractPluginConfig
	{
		private const string DEFAULT_EXPORT_FOLDER = "Export";
		private const string DEFAULT_MATERIAL_SAVE_FOLDER = "Materials";
		private const string DEFAULT_RENDER_IMAGE_SAVE_FOLDER = "Render Images";
		private const string DEFAULT_NORMAL_SAVE_FOLDER = "Normal Maps";
		private const string DEFAULT_GRADIENT_SAVE_FOLDER = "Gradients";
		private const string DEFAULT_ATLASES_SAVE_FOLDER = "Atlases";
		private const string DEFAULT_NOISES_SAVE_FOLDER = "Noises";
		
		private const string MATERIAL_PRESETS_FOLDER_NAME = "MaterialPresets";
		private const string MATERIAL_PRESET_COLLECTION_ASSET_NAME = "MaterialPresetCollection";
		private const string BLENDING_MODE_COLLECTION_ASSET_NAME = "BlendingModeCollection";
		private const string EFFECTS_EXTRA_DATA_ASSET_NAME = "EffectsExtraData";
		private const string PROPERTIES_CONFIG_COLLECTION_ASSET_NAME = "PropertiesConfigCollection";

		public PrefsKeys prefsKeys;

		public MaterialPreset defaultPreset;
		public MaterialPresetCollection materialPresetCollection;

		//Blending Mode Collection
		public BlendingPresetCollection blendingPresetCollection;

		//Effects extra data
		public EffectsExtraData effectsExtraData;

		//Default Names
		public string materialNameDefault;

		//Paths
		public string exportFolderPath		= string.Empty;
		public string materialSavePath		= string.Empty; 
		public string renderImageSavePath	= string.Empty;
		public string normalMapSavePath		= string.Empty;
		public string gradientSavePath		= string.Empty;
		public string atlasesSavePath		= string.Empty;
		public string noiseSavePath			= string.Empty;
		public string bakedShaderSavePath	= string.Empty;

		public float renderImageScale;
		public bool projectConfiguredFirstTime;

		#region HELPER PROPERTIES

		//Derived like RootPluginPath: AllIn1SharedExtensions sits inside the shared folder, next to every plugin
		public string Shared3DRootFolderPath => EditorUtils.CombineAssetPath(EditorUtils.GetAssetParentFolder(RootPluginPath),
			GlobalConstants.FOLDER_NAME_ALLIN1_SHARED, Constants.FOLDER_NAME_SHARED_EXTENSIONS);

		public string Shared3DEditorFolderPath => EditorUtils.CombineAssetPath(Shared3DRootFolderPath, GlobalConstants.FOLDER_NAME_EDITOR);

		public string ShadersFolderPath => EditorUtils.CombineAssetPath(RootPluginPath, Constants.FOLDER_NAME_SHADERS);

		public string ShaderLibraryFolderPath => EditorUtils.CombineAssetPath(ShadersFolderPath, Constants.FOLDER_NAME_SHADER_LIBRARY);

		public string ShaderFeaturesTemplatePath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath
				(
					Shared3DRootFolderPath,
					GlobalConstants.FOLDER_NAME_EDITOR,
					Constants.FOLDER_NAME_EFFECTS_PROFILES,
					Constants.FOLDER_NAME_TEMPLATES,
					Constants.FILE_NAME_SHADER_FEATURES_TEMPLATE
				);

				return res;
			}
		}

		public string ShaderFeaturesPath => EditorUtils.CombineAssetPath(ShaderLibraryFolderPath, $"{GetPluginName()}{Constants.SUFFIX_SHADER_FEATURES}{Constants.EXTENSION_HLSL}");

		public string GenericShadersFolderPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(RootPluginPath, "Shaders/Generic Shaders");
				return res;
			}
		}

		public string GlobalTemplatesFolderPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(Shared3DEditorFolderPath, Constants.FOLDER_NAME_TEMPLATES);
				return res;
			}
		}

		public string EffectsProfileTemplatesFolderPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(Shared3DEditorFolderPath, Constants.FOLDER_NAME_EFFECTS_PROFILES, Constants.FOLDER_NAME_TEMPLATES);
				return res;
			}
		}

		public string ShaderTemplatePath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(EffectsProfileTemplatesFolderPath, Constants.FILE_NAME_SHADER_TEMPLATE);
				return res;
			}
		}

		public string ShaderPropertiesPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(GlobalTemplatesFolderPath, Constants.FILE_NAME_SHADER_PROPERTIES);
				return res;
			}
		}

		public string URPTemplatePath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(EffectsProfileTemplatesFolderPath, Constants.FILE_NAME_PASS_TEMPLATE_URP);
				return res;
			}
		}

		public string BIRPTemplatePath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(EffectsProfileTemplatesFolderPath, Constants.FILE_NAME_PASS_TEMPLATE_BIRP);
				return res;
			}
		}

		public string HLSLTemplateFilePath => EditorUtils.CombineAssetPath(Shared3DEditorFolderPath, Constants.FOLDER_NAME_TEMPLATES, Constants.FILE_NAME_HLSL_LIBRARY_TEMPLATE);

		public string FeturesURPDefinesFilesPath => EditorUtils.CombineAssetPath(ShadersFolderPath, Constants.FOLDER_NAME_SHADER_LIBRARY, $"{GetPluginName()}_{Constants.FILE_NAME_FEATURES_URP_DEFINES}");

		public string MaterialPresetsPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(EditorFolderPath, MATERIAL_PRESETS_FOLDER_NAME);
				return res;
			}
		}

		public string MaterialPresetCollectionAssetPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(MaterialPresetsPath, MATERIAL_PRESET_COLLECTION_ASSET_NAME + ".asset");
				return res;
			}
		}

		public string PropertiesConfigCollectionAssetPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(PluginConfigFolderPath, $"{PROPERTIES_CONFIG_COLLECTION_ASSET_NAME}.asset");
				return res;
			}
		}

		public string EffectsProfileCollectionAssetPath => EditorUtils.CombineAssetPath(PluginConfigFolderPath, $"{EffectsProfileCollection.ASSET_NAME}.asset");

		public string BlendingModeCollectionAssetPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(Shared3DRootFolderPath, GlobalConstants.FOLDER_NAME_EDITOR, $"Presets/Blending/{BLENDING_MODE_COLLECTION_ASSET_NAME}.asset");
				return res;
			}
		}

		public string EffectsExtraDataAssetPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(EditorFolderPath, $"Data/{EFFECTS_EXTRA_DATA_ASSET_NAME}.asset");
				return res;
			}
		}

		public string UrpSettingsAssetPath => EditorUtils.CombineAssetPath(EditorFolderPath, Constants.FOLDER_NAME_URP_SETTINGS, $"{Constants.ASSET_NAME_URP_SETTINGS}{Constants.EXTENSION_ASSET}");

		public string URPSettingsUserPrefAssetPath => EditorUtils.CombineAssetPath(PluginConfigFolderPath, $"{Constants.ASSET_NAME_URP_SETTINGS_USER_PREF}{Constants.EXTENSION_ASSET}");

		//public string InspectorImageName
		//{
		//	get
		//	{
		//		string res = GetPluginName() + "_" + GlobalConstants.STR_CUSTOM_EDITOR_HEADER + ".png";
		//		return res;
		//	}
		//}

		#endregion

		#region FILE_PATHS
		public string BIRPHelperPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(ShaderLibraryFolderPath, Constants.FILE_NAME_BIRP_HELPER);
				return res;
			}
		}

		public string URPHelperPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(ShaderLibraryFolderPath, Constants.FILE_NAME_URP_HELPER);
				return res;
			}
		}
		#endregion

		#region EXPORT PATHS

		//The stored path when it exists on disk, otherwise the default under ExportFolderPath
		protected string ResolveSavePath(string storedPath, string defaultFolderName)
		{
			string res = EditorUtils.NormalizeAssetPath(storedPath);

			if (!EditorUtils.AssetFolderExists(res))
			{
				res = EditorUtils.CombineAssetPath(ExportFolderPath, defaultFolderName);
			}

			return res;
		}

		//Outside the plugin folder, which customers delete to update. Develop keeps it beside the develop config, where its content already is
		public string ExportFolderPath
		{
			get
			{
				string res = string.Empty;
#if ALLIN1_DEVELOP
				res = EditorUtils.CombineAssetPath(PluginConfigFolderPath, DEFAULT_EXPORT_FOLDER);
#else
				res = EditorUtils.CombineAssetPath(GlobalConstants.FOLDER_NAME_ASSET, GlobalConstants.FOLDER_NAME_EXPORTS, GetPluginName());
#endif
				return res;
			}
		}

		public string MaterialSavePath
		{
			get
			{
				return ResolveSavePath(materialSavePath, DEFAULT_MATERIAL_SAVE_FOLDER);
			}
			set
			{
				SetSavePath(ref materialSavePath, value);
			}
		}

		public string NoiseSavePath
		{
			get
			{
				return ResolveSavePath(noiseSavePath, DEFAULT_NOISES_SAVE_FOLDER);
			}
			set
			{
				SetSavePath(ref noiseSavePath, value);
			}
		}

		public string NormalMapSavePath
		{
			get
			{
				return ResolveSavePath(normalMapSavePath, DEFAULT_NORMAL_SAVE_FOLDER);
			}
			set
			{
				SetSavePath(ref normalMapSavePath, value);
			}
		}

		public string GradientSavePath
		{
			get
			{
				return ResolveSavePath(gradientSavePath, DEFAULT_GRADIENT_SAVE_FOLDER);
			}
			set
			{
				SetSavePath(ref gradientSavePath, value);
			}
		}

		public string AtlasesSavePath
		{
			get
			{
				return ResolveSavePath(atlasesSavePath, DEFAULT_ATLASES_SAVE_FOLDER);
			}
			set
			{
				SetSavePath(ref atlasesSavePath, value);
			}
		}

		public string BakedShaderSavePath
		{
			get
			{
				return ResolveSavePath(bakedShaderSavePath, Constants.FOLDER_NAME_BAKED_SHADERS);
			}
			set
			{
				SetSavePath(ref bakedShaderSavePath, value);
			}
		}

		public string ShaderPassCollectionPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(EditorFolderPath, Constants.FOLDER_NAME_SHADER_PASSES, Constants.FILE_NAME_SHADER_PASS_COLLECTION);
				return res;
			}
		}

		public float RenderImageScale
		{
			get
			{
				return renderImageScale;
			}
			set
			{
				if (renderImageScale == value) return;

				renderImageScale = value;
				MarkDirtyIfAlive();
			}
		}

		public string RenderImageSavePath
		{
			get
			{
				return ResolveSavePath(renderImageSavePath, DEFAULT_RENDER_IMAGE_SAVE_FOLDER);
			}
			set
			{
				SetSavePath(ref renderImageSavePath, value);
			}
		}

		protected override void NormalizeSavePaths()
		{
			base.NormalizeSavePaths();

			bool changed = false;

			changed |= NormalizeStoredPath(ref exportFolderPath);
			changed |= NormalizeStoredPath(ref materialSavePath);
			changed |= NormalizeStoredPath(ref renderImageSavePath);
			changed |= NormalizeStoredPath(ref normalMapSavePath);
			changed |= NormalizeStoredPath(ref gradientSavePath);
			changed |= NormalizeStoredPath(ref atlasesSavePath);
			changed |= NormalizeStoredPath(ref noiseSavePath);
			changed |= NormalizeStoredPath(ref bakedShaderSavePath);

			if (changed)
			{
				MarkDirtyIfAlive();
			}
		}
		#endregion


		//Keys
		public string lastTimeShaderPropertiesRebuiltKey;

		//Effects Profile Collection
		public EffectsProfileCollection effectsProfileCollection;

		//PropertiesConfigCollection
		public PropertiesConfigCollection propertiesConfigCollection;

		//Shaders
		public Shader shStandard;
		public Shader shStandardNoShadowCaster;
		public Shader shOutline;
		public Shader shOutlineNoShadowCaster;

		//Assembly Asset

		//Shader Resolver
		public ShaderResolver shaderResolver;

		//URP Settings
		public URPSettings urpSettings;
		public URPSettingsUserPref urpSettingsUserPref;

		//Shaders Creator Tool
		public ShadersCreatorTool shadersCreatorTool;

		public abstract bool IsDerivedFromAllIn13DShader();

		public virtual bool ClearsOptionKeywordsOfDisabledEffects => false;

		public override void Init()
		{
			base.Init();

			RefreshShadersReferences();

			if (this.shadersCreatorTool == null)
			{
				this.shadersCreatorTool = CreateShadersCreatorTool();
			}

			if (materialPresetCollection == null)
			{
				this.materialPresetCollection = AssetDatabase.LoadAssetAtPath<MaterialPresetCollection>(MaterialPresetCollectionAssetPath);
			}

			if (blendingPresetCollection == null)
			{
				this.blendingPresetCollection = AssetDatabase.LoadAssetAtPath<BlendingPresetCollection>(BlendingModeCollectionAssetPath);
			}

			if (effectsExtraData == null)
			{
				this.effectsExtraData = AssetDatabase.LoadAssetAtPath<EffectsExtraData>(EffectsExtraDataAssetPath);
			}

			if (IsDerivedFromAllIn13DShader())
			{
				if (defaultPreset == null)
				{
					defaultPreset = materialPresetCollection[0];
				}

				bool collectionsCreated = EnsureCollections();

				if (urpSettings == null)
				{
					this.urpSettings = AssetDatabase.LoadAssetAtPath<URPSettings>(UrpSettingsAssetPath);
				}

				//Plugins without a URP Settings tab, like the Text Toolkit, ship no URPSettings asset
				if (urpSettings != null && urpSettingsUserPref == null)
				{
					this.urpSettingsUserPref = AssetDatabase.LoadAssetAtPath<URPSettingsUserPref>(URPSettingsUserPrefAssetPath);

					if (urpSettingsUserPref == null)
					{
						this.urpSettingsUserPref = URPSettingsUserPref.CreateAsset(this);
					}
				}

				if (!collectionsCreated)
				{
					BindProfiles();
				}
			}
		}

		//Neither collection is saved on the config, so a reload can leave them null long after Init ran.
		//Whoever is about to read them calls this first. Returns whether anything had to be created
		public bool EnsureCollections()
		{
			if (!IsDerivedFromAllIn13DShader()) return false;

			bool created = false;

			if (propertiesConfigCollection == null)
			{
				if (shadersCreatorTool == null)
				{
					shadersCreatorTool = CreateShadersCreatorTool();
				}

				shadersCreatorTool.BuildShaderFiles(this);
				propertiesConfigCollection = PropertiesConfigCreator.CreateConfig(this);
				created = true;
			}

			if (effectsProfileCollection == null)
			{
				//The stored one keeps the user's profiles. BindProfiles below reattaches it to the current effect configs
				effectsProfileCollection = AssetDatabase.LoadAssetAtPath<EffectsProfileCollection>(EffectsProfileCollectionAssetPath);

				if (effectsProfileCollection == null)
				{
					effectsProfileCollection = EffectsProfileCollection.CreateAsset(propertiesConfigCollection, this);
				}

				created = true;
			}

			if (created)
			{
				BindProfiles();
			}

			return created;
		}

		private void BindProfiles()
		{
			PropertiesConfig propertiesConfig = propertiesConfigCollection.GetPropertiesConfig();
			effectsProfileCollection.BindEffectConfigs(propertiesConfig);

			effectsProfileCollection.CleanInvalidProfiles();
			AdditionalActionsOverEffectsProfiles(propertiesConfig);
		}

		public override void OnAssetsProcessed(string[] importedAssets, string[] deletedAssets)
		{
			//Reacting to a deletion only matters for state that is already built
			if (!initialized)
			{
				return;
			}

			for (int i = 0; i < deletedAssets.Length; i++)
			{
				if (deletedAssets[i].EndsWith(".shader") && effectsProfileCollection != null)
				{
					effectsProfileCollection.CheckRemovedShader(deletedAssets[i]);
				}
			}

			//Shader changes are handled here (and not in Init) so they also propagate mid-session.
			//The content-compare inside BuildShaderFiles makes the rebuild triggered by our own
			//generated shader imports a no-op, so this converges instead of looping
			if (HasShadersChanged(importedAssets, deletedAssets))
			{
				//ShadersCreatorTool is not serializable, so this field is null again after every domain reload
				if (shadersCreatorTool == null)
				{
					shadersCreatorTool = CreateShadersCreatorTool();
				}

				shadersCreatorTool.BuildShaderFiles(this);
				propertiesConfigCollection = PropertiesConfigCreator.CreateConfig(this);

				//The profiles still point at the effect configs of the collection just replaced
				if (effectsProfileCollection != null)
				{
					BindProfiles();
				}
			}
		}

		public string GetPassConfigPath(AllIn13DPassType passType)
		{
			string passSuffix = string.Empty;
			switch (passType)
			{
				case AllIn13DPassType.DEPTH_NORMALS:
					passSuffix = "DepthNormals";
					break;
				case AllIn13DPassType.DEPTH_ONLY:
					passSuffix = "DepthOnly";
					break;
				case AllIn13DPassType.FORWARD_ADD:
					passSuffix = "ForwardAdd";
					break;
				case AllIn13DPassType.MAIN:
					passSuffix = "Main";
					break;
				case AllIn13DPassType.META:
					passSuffix = "Meta";
					break;
				case AllIn13DPassType.NONE:
					passSuffix = "";
					break;
				case AllIn13DPassType.OUTLINE:
					passSuffix = "Outline";
					break;
				case AllIn13DPassType.SHADOW_CASTER:
					passSuffix = "ShadowCaster";
					break;
			}

			string fileName = $"ShaderPass_{passSuffix}.asset";
			string res = EditorUtils.CombineAssetPath(EditorFolderPath, Constants.FOLDER_NAME_SHADER_PASSES, fileName);

			return res; 
		}

		public string GetPassHLSLFilePath(AllIn13DPassType passType)
		{
			string passSuffix = string.Empty;
			switch (passType)
			{
				case AllIn13DPassType.DEPTH_NORMALS:
					passSuffix = "URP_DepthNormalsPass";
					break;
				case AllIn13DPassType.DEPTH_ONLY:
					passSuffix = "URP_DepthOnlyPass";
					break;
				case AllIn13DPassType.FORWARD_ADD:
					passSuffix = "AddPass";
					break;
				case AllIn13DPassType.MAIN:
					passSuffix = "BasePass";
					break;
				case AllIn13DPassType.META:
					passSuffix = "URP_MetaPass";
					break;
				case AllIn13DPassType.NONE:
					passSuffix = "";
					break;
				case AllIn13DPassType.OUTLINE:
					passSuffix = "OutlinePass";
					break;
				case AllIn13DPassType.SHADOW_CASTER:
					passSuffix = "ShadowCasterPass";
					break;
			}

			string fileName = $"{GetPluginName()}_{passSuffix}.hlsl";
			string res = EditorUtils.CombineAssetPath(ShaderLibraryFolderPath, fileName);

			return res;
		}

		protected virtual void AdditionalActionsOverEffectsProfiles(PropertiesConfig propertiesConfig)
		{

		}

		private bool HasShadersChanged(string[] importedAssets, string[] deletedAssets)
		{
			bool res = false;

			string[] shaderNames = GetShaderNames();
			for (int i = 0; i < shaderNames.Length; i++)
			{
				//importedAssets is always forward slashed, so a backslash here would never match
				string shaderFilePath = GetShaderFilePath(shaderNames[i]);

				res = res || importedAssets.Contains(shaderFilePath) || deletedAssets.Contains(shaderFilePath);
			}

			return res;
		}

		protected virtual void RefreshShadersReferences()
		{
			shStandard = null;
			shStandardNoShadowCaster = null;
			shOutline = null;
			shOutlineNoShadowCaster = null;
		}

		public string GetRootPath()
		{
			return RootPluginPath;
		}

		public string GetMainShaderName()
		{
			string[] shaderNames = GetShaderNames();

			string res = shaderNames[0];
			return res;
		}

		public string GetMainShaderFullName()
		{
			string res = $"{GetPluginName()}/{GetMainShaderName()}";
			return res;
		}

		public string GetShaderFilePath(string shaderName)
		{
			string res = EditorUtils.CombineAssetPath(GenericShadersFolderPath, $"{shaderName}.shader");
			return res;
		}

		public void Refresh()
		{
			prefsKeys = new PrefsKeys(this);
		}

		protected override void CreateFolders()
		{
			base.CreateFolders();

			EditorUtils.CreateFolderIfNotExist(ExportFolderPath);
			EditorUtils.CreateFolderIfNotExist(MaterialSavePath);
			EditorUtils.CreateFolderIfNotExist(NoiseSavePath);
			EditorUtils.CreateFolderIfNotExist(NormalMapSavePath);
			EditorUtils.CreateFolderIfNotExist(GradientSavePath);
			EditorUtils.CreateFolderIfNotExist(AtlasesSavePath);
			EditorUtils.CreateFolderIfNotExist(RenderImageSavePath);
			EditorUtils.CreateFolderIfNotExist(BakedShaderSavePath);
		}

		public virtual ShadersCreatorTool CreateShadersCreatorTool()
		{
			ShadersCreatorTool res = new ShadersCreatorTool();
			return res;
		}

		public virtual PropertiesConfig CreatePropertiesConfig()
		{
			PropertiesConfig res = new PropertiesConfig();
			return res;
		}

		public virtual PropertiesConfigCollection CreatePropertiesConfigSO()
		{
			return null;
		}


#if ALLIN1_DEVELOP
		public override string[] GetFoldersToExport()
		{
			string[] res = base.GetFoldersToExport();

			ArrayUtility.Add(ref res, Shared3DRootFolderPath);

			return res;
		}
#endif

		public Shader GetShaderBase()
		{
			return shStandard;
		}

		public override Material GetDefaultMaterial()
		{
			return defaultPreset.material;
		}

		public override string GetMaterialSavePath()
		{
			return MaterialSavePath;
		}
	}
}