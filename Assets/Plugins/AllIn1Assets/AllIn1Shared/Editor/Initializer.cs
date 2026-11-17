using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AllIn1Assets
{
	public class Initializer : AssetPostprocessor 
	{
		private class Values
		{
			public bool leftoversCleaned;
		}

		private const string TAG = "Initializer";

		//Leftovers from switching between develop and release builds. Cleaned on first real use, not
		//from the import callback: it needs the configs, and during an import they cannot answer
		//private static bool leftoversCleaned;

		private static readonly Values values = new Values();

		public static void EnsureLeftoversCleaned()
		{
			if (values.leftoversCleaned) return;

			values.leftoversCleaned = true;

			CheckDefineSymbol();
		}

		//The one thing the lazy model still owes at load: asking about the render pipeline when some
		//plugin has no answer stored. Deferred past the load callback, where creating assets is
		//allowed, and read only unless the question is actually due. Not compiled on the very first
		//import into a URP project, where RenderPipelineChecker writes the define on this reload: the
		//recompile that causes runs it, so the reconcile never needs to know which reload it is on
		[InitializeOnLoadMethod]
		private static void ScheduleRenderPipelineReconcile()
		{
			if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Secondary)
				return;

#if ALLIN1_URP
			EditorApplication.delayCall += URPConfigurator.Reconcile;
#endif
		}

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
		{
			//Secondary processes (standalone profiler) have no usable AssetDatabase
			if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Secondary)
				return;

#if ALLIN1_DEVELOP
			LogAssetsProcessed(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths, didDomainReload);
#endif

			/**/
			CheckPostprocessLibrary(deletedAssets);
			/**/

			//Creating, saving and importing assets from inside an import callback is unsupported, defer past the import phase
			EditorApplication.delayCall += () => ProcessAllAssetsSafe(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths, didDomainReload);
		}

		private static void CheckPostprocessLibrary(string[] deletedAssets)
		{
#if ALLIN1_BIRP
			bool isPostprocesingRemoved = false;
			for (int i = 0; i < deletedAssets.Length; i++)
			{
				if (deletedAssets[i].Contains(@"Packages/com.unity.postprocessing"))
				{
					isPostprocesingRemoved = true;
					break;
				}
			}
			if (isPostprocesingRemoved)
			{
				SharedEditorUtils.RemoveSymbol("UNITY_POST_PROCESSING_STACK_V2");
			}
#endif
		}

		//This also runs while a player build reimports assets, so setup must never abort the build
		private static void ProcessAllAssetsSafe(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
		{
			try
			{
				ProcessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths, didDomainReload);
			}
			catch (Exception e)
			{
				//Debug, not AllIn1Logger, which is [Conditional("ALLIN1_LOG")] and vanishes for users
				UnityEngine.Debug.LogWarning("[AllIn1] Plugin setup did not complete. This does not stop your build. " +
					"Check the Save Paths tab of the plugin window for invalid folders. " +
					$"Details: {e}");
			}
		}

		//Deliberately small. Setup that reads the project belongs to whoever needs it, so it can run
		//once the import has finished: anything resolved from here is resolved too early to be right
		private static void ProcessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
		{
			//Configs that resolved mid import could not save then. This batch runs after it, so now they can
			PluginRegistry.RetryPendingSaves();

			//A deleted config comes back right away instead of waiting for its next use
			PluginRegistry.NotifyAssetsDeleted(deletedAssets);

			//Only configs already in use this session react to what changed. Nothing resolves here
			List<AbstractPluginConfig> resolvedPlugins = PluginRegistry.GetPlugins();

			for (int i = 0; i < resolvedPlugins.Count; i++)
			{
				resolvedPlugins[i].OnAssetsProcessed(importedAssets, deletedAssets);
			}
		}

		public static void CheckDefineSymbol()
		{
			Debug.Log("Checking define symbols!");

#if ALLIN1_DEVELOP
			List<AbstractPluginConfig> plugins = PluginRegistry.ResolveAll();

			for(int i = 0; i < plugins.Count; i++)
			{
				EditorUtils.DeleteFolderIfExist(plugins[i].ReleasePluginConfigFolderPath);
			}

			AllIn1Logger.Log($"Trying to remove {PluginRegistry.GetSharedConfig().AssetPathRelease}");
			EditorUtils.DeleteAssetIfExist(PluginRegistry.GetSharedConfig().AssetPathRelease);
#else
			EditorUtils.DeleteFolderIfExist(PluginRegistry.GetSharedConfig().UserConfigFolderDevelop);
			EditorUtils.DeleteAssetIfExist(PluginRegistry.GetSharedConfig().AssetPathDevelop);
#endif
		}

#if ALLIN1_DEVELOP
		private static void LogAssetsProcessed(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
		{
			string logString = string.Empty;

			logString += $"Did domain reload? {didDomainReload}\n";
			logString = LogAssetCollection("Imported", importedAssets, logString);
			logString = LogAssetCollection("Deleted", deletedAssets, logString);
			logString = LogAssetCollection("Moved", movedAssets, logString);
			logString = LogAssetCollection("Moved From Asset Paths", movedFromAssetPaths, logString);

			AllIn1Logger.Log(logString);
		}

		private static string LogAssetCollection(string collectionName, string[] collection, string target)
		{
			string res = target;

			res += collectionName;
			res += "\n";
			res += "----------------------------";
			for (int i = 0; i < collection.Length; i++)
			{
				res += "\n";
				res += collection[i];
				res += "\n";
			}
			res += "----------------------------";
			res += "\n";

			return res;
		}
#endif
	}
}