#if ALLIN1_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AllIn1Assets
{
	public abstract class AbstractPluginConfig : ScriptableObject
	{
		private const string TAG = "AbstractPluginConfig";

		private const string ASSET_NAME_FORMAT = "{0}.asset";
		private const string ASSEMBLY_NAME_FORMAT = "{0}Assembly";

		//The plugin's answer to the URP configuration question, accepted or declined. Serialized: it is
		//the state the load reconcile reads, and a config that does not exist counts as no answer yet
		public bool urpConfiguredFirstTime;

		public bool sceneNotificationsEnabled;

		//Derived from where the files are, so it is never serialized: a stored copy can only be stale
		//or, when it was resolved mid import, wrong forever. Memoized per session once it answers
		[System.NonSerialized] private string resolvedRootPluginPath;
		[System.NonSerialized] private Texture resolvedInspectorImage;


		#region PATHS
		public string RootPluginPath
		{
			get
			{
				if (string.IsNullOrEmpty(resolvedRootPluginPath))
				{
					resolvedRootPluginPath = ResolveRootPluginPath();
				}

				return resolvedRootPluginPath;
			}
		}

		public string ReleasePluginConfigFolderPath => EditorUtils.CombineAssetPath(RootPluginPath, GlobalConstants.FOLDER_NAME_CONFIG);

		//CombineAssetPath, not Path.Combine: this feeds AssetDatabase, which rejects Windows backslashes
		public string DevelopPluginConfigFolderPath => EditorUtils.CombineAssetPath(
			EditorUtils.CombineAssetPath(GlobalConstants.FOLDER_NAME_ASSET, GlobalConstants.FOLDER_NAME_DEVELOP_CONFIG_PARENT),
			$"{GetPluginName()}{GlobalConstants.SUFFIX_DEVELOP_CONFIG}");


		public string PluginConfigFolderPath
		{
			get
			{
				string res = string.Empty;
#if ALLIN1_DEVELOP
				res = DevelopPluginConfigFolderPath;
#else
				res = ReleasePluginConfigFolderPath;
#endif
				return res;
			}
		}

		public string ConfigAssetPath => EditorUtils.CombineAssetPath(PluginConfigFolderPath, GetAssetName());

		public string DemoFolderPath => EditorUtils.CombineAssetPath(RootPluginPath, GlobalConstants.FOLDER_NAME_DEMO);

		public string EditorFolderPath => EditorUtils.CombineAssetPath(RootPluginPath, GlobalConstants.FOLDER_NAME_EDITOR);

		public string InspectorImageName => $"{GetPluginName()}_{GlobalConstants.STR_CUSTOM_EDITOR_HEADER}.png";

		public string InspectorImagePath => EditorUtils.CombineAssetPath(EditorUtils.CombineAssetPath(EditorFolderPath, "Texture"), InspectorImageName);

		#endregion


		//Session scoped: the setup below builds objects that no domain reload can carry over
		[System.NonSerialized] public bool initialized;
		[System.NonSerialized] private bool initializing;

		//An instance with no asset behind it yet. Its paths already answer, since they derive from
		//its own script, so the registry can use it to look for the stored asset
		public static AbstractPluginConfig CreateTransientInstance(System.Type configType)
		{
			return ScriptableObject.CreateInstance(configType) as AbstractPluginConfig;
		}

		public void PrepareAsNewConfig()
		{
			sceneNotificationsEnabled = true;

			SaveAsAsset();
		}

		//Called by whoever needs the config, never by the asset postprocessor. Setup that reads the
		//project has to run when someone asks for it, not while the assets are still importing
		public void EnsureInitialized()
		{
			//Init reaches back into the registry in places, and that would re-enter here
			if (initialized || initializing) return;

			initializing = true;

			try
			{
				Initializer.EnsureLeftoversCleaned();

				CreateFolders();
				Init();

				initialized = true;
			}
			finally
			{
				initializing = false;
			}
		}

		//Called on every asset postprocess batch, unlike Init which only runs once per session
		public virtual void OnAssetsProcessed(string[] importedAssets, string[] deletedAssets)
		{

		}

		public virtual void Init()
		{
			AllIn1Logger.LogWarning(TAG, $"{GetPluginName()} initializing");

			NormalizeSavePaths();
		}

		public bool SaveAsAsset()
		{
			AllIn1Logger.Log("Saving in " + ConfigAssetPath);

			//AssetDatabase.CreateAsset only logs "Couldn't create asset file!" when the folder is not there
			if(!EditorUtils.EnsureAssetFolderExists(PluginConfigFolderPath))
			{
				Debug.LogWarning($"[AllIn1] Could not save the {GetPluginName()} configuration in '{PluginConfigFolderPath}'. " +
					"The plugin keeps working and will save it once the import finishes.");

				return false;
			}

			AssetDatabase.CreateAsset(this, ConfigAssetPath);

			return AssetDatabase.Contains(this);
		}

		public string GetAssetName()
		{
			string res = string.Format(ASSET_NAME_FORMAT, GetPluginName());
			return res;
		}

		public string GetAssemblyName()
		{
			string res = string.Format(ASSEMBLY_NAME_FORMAT, GetPluginName());
			return res;
		}

		public string GetAssemblyNameWithExtension()
		{
			string res = $"{GetAssemblyName()}.{GlobalConstants.EXTENSION_ASMDEF}";
			return res;
		}

		public bool IsProjectAsset()
		{
			bool res = AssetDatabase.IsMainAsset(this);
			return res;
		}

		public Texture GetInspectorImage()
		{
			if (resolvedInspectorImage == null)
			{
				resolvedInspectorImage = AssetDatabase.LoadAssetAtPath<Texture>(InspectorImagePath);
			}

			return resolvedInspectorImage;
		}

		public virtual Material GetDefaultMaterial()
		{
			return null;
		}

		public void ShowNotification(string message, double fadeOutWait = 1.0)
		{
			if (sceneNotificationsEnabled)
			{
				EditorUtils.ShowNotification(message, fadeOutWait);
			}
		}

		//The plugin root folder is named after the plugin, and a script path resolves through its own
		//GUID. No asset database search, so this also answers while assets are still importing
		protected virtual string ResolveRootPluginPath()
		{
			MonoScript ownScript = MonoScript.FromScriptableObject(this);
			string folderPath = EditorUtils.GetAssetParentFolder(EditorUtils.NormalizeAssetPath(AssetDatabase.GetAssetPath(ownScript)));

			string folderSuffix = $"/{GetPluginName()}";
			while(!string.IsNullOrEmpty(folderPath))
			{
				if(folderPath.EndsWith(folderSuffix)) return folderPath;

				folderPath = EditorUtils.GetAssetParentFolder(folderPath);
			}

			return string.Empty;
		}

		//Override to repair serialized paths a Windows session wrote with backslashes
		protected virtual void NormalizeSavePaths()
		{
		}

		protected bool NormalizeStoredPath(ref string path)
		{
			string normalizedPath = EditorUtils.NormalizeAssetPath(path);

			bool res = normalizedPath != path;
			path = normalizedPath;

			return res;
		}

		//Path setters run from IMGUI, so they fire every repaint and can outlive the asset when a window stays open
		protected void SetSavePath(ref string storedPath, string value)
		{
			string normalizedPath = EditorUtils.NormalizeAssetPath(value);

			if (storedPath == normalizedPath) return;

			storedPath = normalizedPath;
			MarkDirtyIfAlive();
		}

		protected void MarkDirtyIfAlive()
		{
			if (this == null) return;

			EditorUtility.SetDirty(this);
		}

		protected virtual void CreateFolders()
		{
			EditorUtils.CreateFolderIfNotExist(PluginConfigFolderPath);
		}

		public virtual string GetMaterialSavePath()
		{
			return string.Empty;
		}

		public bool IsURPConfiguredFirstTime()
		{
			return urpConfiguredFirstTime;
		}

		//False for a plugin with no shaders: it never raises the URP prompt nor takes part in the automatic configuration
		public virtual bool NeedsURPConfiguration()
		{
			return true;
		}

		//The manual page the URP prompt links when this plugin raised it
		public virtual string GetRenderPipelineDocURL()
		{
			return GlobalConstants.URL_DOCS_SITE;
		}

		//Dirtied so the answer survives the editor closing before the next save
		private void MarkURPAnswered()
		{
			if (urpConfiguredFirstTime) return;

			urpConfiguredFirstTime = true;
			MarkDirtyIfAlive();
		}

		public abstract Shader GetShaderAfterMaterialRemoval();

		public bool IsMaterialFromThisPlugin(Material mat)
		{
			bool res = false;

			string[] shaderNames = GetShaderNames();
			if (shaderNames != null)
			{
				for (int i = 0; i < shaderNames.Length; i++)
				{
					if (mat.shader.name.Contains(shaderNames[i]))
					{
						res = true;
						break;
					}
				}
			}

			return res;
		}

		public abstract PluginID GetPluginID();

		public abstract string GetPluginName();

		public abstract string[] GetShaderNames();

		public abstract string[] GetShaderTemplates();

		public abstract string GetVersion();

		public abstract bool IsWritableFolder(string folderPath);

#if ALLIN1_URP
		public abstract void ConvertMaterialsFolder();

		public abstract void ConfigureRenderPipeline(UniversalRendererData universalRendererData, RenderPipelineAsset renderPipelineAsset);

		public virtual void ConfigureURPAccepted(RenderPipelineAsset renderPipelineAsset)
		{
			MarkURPAnswered();
		}

		public virtual void ConfigureURPDenied(RenderPipelineAsset renderPipelineAsset)
		{
			MarkURPAnswered();
		}
#endif

#if ALLIN1_DEVELOP
		public virtual string[] GetFoldersToExport()
		{
			string[] res = new string[]
			{
				PluginRegistry.GetSharedConfig().AllIn1SharedAssemblyPath,
				PluginRegistry.GetSharedConfig().AllIn1SharedDemoFolderPath,
				PluginRegistry.GetSharedConfig().AllIn1SharedEditorFolderPath,
				PluginRegistry.GetSharedConfig().AllIn1SharedScriptsFolderPath,

				RootPluginPath,
			};

			return res;
		}

		public string GetPackageName()
		{
			string res = $"{GetPluginName()}_{System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}";
			return res;
		}

		//Content that reaches customers through a nested package instead of the main export
		public virtual string[] GetAssetPathsToExcludeFromExport()
		{
			return new string[0];
		}
#endif

	}
}