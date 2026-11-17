using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AllIn1Assets
{
	public class SharedConfig : ScriptableObject
	{
		private const string ASSET_NAME = "AllIn1_SharedConfig";

		//Derived from where this script sits, never serialized: a stored path can only be stale, and a
		//stored path resolved mid import is wrong forever
		[System.NonSerialized] private string resolvedRootPath;

		public string rootPath
		{
			get
			{
				if (string.IsNullOrEmpty(resolvedRootPath))
				{
					resolvedRootPath = ResolveRootPathFromOwnScript();
				}

				return resolvedRootPath;
			}
		}

		public string assemblyPath => EditorUtils.CombineAssetPath(AllIn1SharedFolderPath,
			$"{GlobalConstants.ASSEMBLY_NAME_ALLIN1_SHARED}.{GlobalConstants.EXTENSION_ASMDEF}");


		public string UserConfigFolderRelease => EditorUtils.NormalizeAssetPath(rootPath);
		public string UserConfigFolderDevelop => EditorUtils.CombineAssetPath(GlobalConstants.FOLDER_NAME_ASSET, GlobalConstants.FOLDER_NAME_DEVELOP_CONFIG_PARENT);

		public string UserConfigFolderPath
		{
			get
			{
				string res = string.Empty;

#if ALLIN1_DEVELOP
				res = UserConfigFolderDevelop;
#else
				res = UserConfigFolderRelease;
#endif
				return res;
			}
		}

		public string AssetPathDevelop => EditorUtils.CombineAssetPath(UserConfigFolderDevelop, $"{ASSET_NAME}.asset");

		public string AssetPathRelease => EditorUtils.CombineAssetPath(UserConfigFolderRelease, $"{ASSET_NAME}.asset");

		public string AssetPath => EditorUtils.CombineAssetPath(UserConfigFolderPath, $"{ASSET_NAME}.asset");

		public string UserConfigFolder
		{
			get
			{
				string res = string.Empty;
#if ALLIN1_DEVELOP
				res = UserConfigFolderDevelop;
#else
				res = UserConfigFolderRelease;
#endif
				return res;
			}
		}


		public string AllIn1SharedFolderPath => EditorUtils.CombineAssetPath(rootPath, GlobalConstants.FOLDER_NAME_ALLIN1_SHARED);
		public string AllIn1SharedDemoFolderPath => EditorUtils.CombineAssetPath(AllIn1SharedFolderPath, GlobalConstants.FOLDER_NAME_DEMO);
		public string AllIn1SharedEditorFolderPath => EditorUtils.CombineAssetPath(AllIn1SharedFolderPath, GlobalConstants.FOLDER_NAME_EDITOR);
		public string AllIn1SharedScriptsFolderPath => EditorUtils.CombineAssetPath(AllIn1SharedFolderPath, GlobalConstants.FOLDER_NAME_SCRIPTS);
		public string AllIn1SharedAssemblyPath => EditorUtils.NormalizeAssetPath(assemblyPath);

		//This script sits in <rootPath>/AllIn1Shared/Editor and its path resolves through its own GUID,
		//so it answers while assets are still importing, which an asset database search does not
		public string ResolveRootPathFromOwnScript()
		{
			MonoScript ownScript = MonoScript.FromScriptableObject(this);
			string ownScriptPath = EditorUtils.NormalizeAssetPath(AssetDatabase.GetAssetPath(ownScript));

			return EditorUtils.GetFolderPathByFilePath(ownScriptPath, 2);
		}

		public bool SaveAsAsset()
		{
			string folderPath = UserConfigFolder;

			//AssetDatabase.CreateAsset only logs "Couldn't create asset file!" when the folder is not
			//there, which said nothing about which folder was missing or that the session still works
			if(!EditorUtils.EnsureAssetFolderExists(folderPath))
			{
				Debug.LogWarning($"[AllIn1] Could not save {ASSET_NAME} in '{folderPath}'. " +
					"The plugin keeps working and will save it once the import finishes.");

				return false;
			}

			AssetDatabase.CreateAsset(this, EditorUtils.CombineAssetPath(folderPath, $"{ASSET_NAME}.asset"));

			return AssetDatabase.Contains(this);
		}
	}
}