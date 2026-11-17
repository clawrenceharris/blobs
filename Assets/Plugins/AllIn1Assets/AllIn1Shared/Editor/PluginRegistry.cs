using System;
using System.Collections.Generic;
using UnityEditor;

#if ALLIN1_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif


namespace AllIn1Assets
{
	//Registration is free: a plugin declares its ID and config type at load and nothing is read from
	//the project. The config asset is found, or created, the first time something actually asks for it
	public static class PluginRegistry
	{
		public class StaticValues
		{
			public SharedConfig resolvedSharedConfig;
			public List<AbstractPluginConfig> plugins;
			public string cachedSharedAssetPath;
			public string cachedSharedAssemblyFileName;


			public StaticValues()
			{
				this.plugins = new List<AbstractPluginConfig>();
			}
		}

		private const string TAG = "PluginRegistry";

		private static readonly StaticValues staticValues = new StaticValues();


		private class Registration
		{
			public PluginID id;
			public Type configType;
			public AbstractPluginConfig instance;
			public string assemblyFileName;
			public string configAssetPath;
		}

		private static readonly List<Registration> registrations = new List<Registration>();

		//private static SharedConfig resolvedSharedConfig;

		//Resolved configs only. Anything that needs them all calls ResolveAll first
		//public static List<AbstractPluginConfig> Plugins
		//{
		//	get
		//	{
		//		List<AbstractPluginConfig> res = new List<AbstractPluginConfig>();

		//		for (int i = 0; i < registrations.Count; i++)
		//		{
		//			if (registrations[i].instance != null)
		//			{
		//				res.Add(registrations[i].instance);
		//			}
		//		}

		//		return res;
		//	}
		//}


		public static List<AbstractPluginConfig> GetPlugins()
		{
			List<AbstractPluginConfig> res = new List<AbstractPluginConfig>();

			for (int i = 0; i < registrations.Count; i++)
			{
				if (registrations[i].instance != null)
				{
					res.Add(registrations[i].instance);
				}
			}

			return res;
		}

		//public static SharedConfig sharedConfig
		//{
		//	get
		//	{
		//		if (staticValues.resolvedSharedConfig == null)
		//		{
		//			staticValues.resolvedSharedConfig = ResolveSharedConfig();
		//		}

		//		return staticValues.resolvedSharedConfig;
		//	}
		//}

		public static SharedConfig GetSharedConfig()
		{
			SharedConfig res = null;

			if (staticValues.resolvedSharedConfig == null)
			{
				staticValues.resolvedSharedConfig = ResolveSharedConfig();
			}

			res = staticValues.resolvedSharedConfig;

			return res;
		}

		public static void Register(PluginID id, Type configType)
		{
			Registration registration = FindRegistration(id);

			if (registration == null)
			{
				registration = new Registration { id = id };
				registrations.Add(registration);
			}

			registration.configType = configType;
		}

		//An instance that already exists, so nothing has to be found or created for it
		public static void AddToRegistry(AbstractPluginConfig entry)
		{
			if (entry == null) return;

			Registration registration = FindRegistration(entry.GetPluginID());

			if (registration == null)
			{
				registration = new Registration { id = entry.GetPluginID() };
				registrations.Add(registration);
			}

			registration.configType = entry.GetType();
			registration.instance = entry;
		}

		//Every window, inspector and tool reaches a config through here, so this is where the asset is
		//resolved and its setup runs: on first use, with an asset database that can answer
		public static T GetPluginByID<T>(PluginID id) where T : AbstractPluginConfig
		{
			AbstractPluginConfig res = Resolve(FindRegistration(id));

			if (res == null) return null;

			res.EnsureInitialized();

			return res as T;
		}

		public static List<AbstractPluginConfig> ResolveAll()
		{
			for (int i = 0; i < registrations.Count; i++)
			{
				Resolve(registrations[i]);
			}

			List<AbstractPluginConfig> res = GetPlugins();
			return res;
		}

		public static List<PluginID> GetRegisteredIDs()
		{
			List<PluginID> res = new List<PluginID>();

			for (int i = 0; i < registrations.Count; i++)
			{
				res.Add(registrations[i].id);
			}

			return res;
		}

		//Load only: what every registered plugin has stored, with null in the slot of a plugin whose
		//config does not exist yet. The load reconcile reads its answers through this, and a load with
		//nothing to ask must leave no asset behind
		public static List<AbstractPluginConfig> PeekAll()
		{
			List<AbstractPluginConfig> res = new List<AbstractPluginConfig>();

			for (int i = 0; i < registrations.Count; i++)
			{
				res.Add(Peek(registrations[i]));
			}

			return res;
		}

		//PeekAll for the URP question: a plugin with nothing to configure for a pipeline is left out, so it never asks
		public static List<AbstractPluginConfig> PeekAllNeedingURPConfiguration()
		{
			List<AbstractPluginConfig> res = new List<AbstractPluginConfig>();

			for (int i = 0; i < registrations.Count; i++)
			{
				AbstractPluginConfig stored = Peek(registrations[i]);

				if (!NeedsURPConfiguration(registrations[i])) continue;

				res.Add(stored);
			}

			return res;
		}

		//ResolveAll for the URP answer: shaderless plugins get no config asset created just to store it
		public static List<AbstractPluginConfig> ResolveAllNeedingURPConfiguration()
		{
			List<AbstractPluginConfig> res = new List<AbstractPluginConfig>();

			for (int i = 0; i < registrations.Count; i++)
			{
				if (!NeedsURPConfiguration(registrations[i])) continue;

				AbstractPluginConfig resolved = Resolve(registrations[i]);
				if (resolved == null) continue;

				res.Add(resolved);
			}

			return res;
		}

		//Same throwaway-instance trick as GetAssemblyFileName when the plugin has no config yet
		private static bool NeedsURPConfiguration(Registration registration)
		{
			if (registration.instance != null) return registration.instance.NeedsURPConfiguration();

			if (registration.configType == null) return false;

			AbstractPluginConfig transient = AbstractPluginConfig.CreateTransientInstance(registration.configType);
			if (transient == null) return false;

			bool res = transient.NeedsURPConfiguration();
			UnityEngine.Object.DestroyImmediate(transient);

			return res;
		}

		private static AbstractPluginConfig Peek(Registration registration)
		{
			if (registration.instance != null) return registration.instance;
			if (registration.configType == null) return null;

			//Kept once found: the next peek and the next resolve both answer from here
			registration.instance = LoadStored(registration.configType);

			return registration.instance;
		}

		//Deleting a config used to regenerate it on the spot, and lazy resolution alone would make it
		//reappear only on next use. This restores the old behavior for an explicit deletion: the batch
		//that carried the delete re-resolves the config, so the asset is back seconds later, in the dev
		//project and in a customer project alike. A plugin being uninstalled is left alone: its own
		//assembly is in the same batch, and recreating the config would resurrect the folder
		public static void NotifyAssetsDeleted(string[] deletedAssets)
		{
			if (deletedAssets == null || deletedAssets.Length == 0) return;

			for (int i = 0; i < registrations.Count; i++)
			{
				string configAssetPath = GetConfigAssetPath(registrations[i]);
				if (string.IsNullOrEmpty(configAssetPath)) continue;

				if (!ContainsPath(deletedAssets, configAssetPath)) continue;
				if (AnyPathEndsWith(deletedAssets, GetAssemblyFileName(registrations[i]))) continue;

				Resolve(registrations[i]);
			}

			if (string.IsNullOrEmpty(staticValues.cachedSharedAssetPath))
			{
				SharedConfig sharedCandidate = UnityEngine.ScriptableObject.CreateInstance<SharedConfig>();
				staticValues.cachedSharedAssetPath = sharedCandidate.AssetPath;
				staticValues.cachedSharedAssemblyFileName = System.IO.Path.GetFileName(sharedCandidate.AllIn1SharedAssemblyPath);
				UnityEngine.Object.DestroyImmediate(sharedCandidate);
			}

			if (ContainsPath(deletedAssets, staticValues.cachedSharedAssetPath) && !AnyPathEndsWith(deletedAssets, staticValues.cachedSharedAssemblyFileName))
			{
				staticValues.resolvedSharedConfig = null;
				_ = GetSharedConfig();
			}
		}

		private static bool ContainsPath(string[] paths, string target)
		{
			for (int i = 0; i < paths.Length; i++)
			{
				if (string.Equals(EditorUtils.NormalizeAssetPath(paths[i]), target, StringComparison.Ordinal)) return true;
			}

			return false;
		}

		private static bool AnyPathEndsWith(string[] paths, string suffix)
		{
			if (string.IsNullOrEmpty(suffix)) return false;

			for (int i = 0; i < paths.Length; i++)
			{
				if (paths[i].EndsWith(suffix, StringComparison.Ordinal)) return true;
			}

			return false;
		}

		//Same throwaway-instance trick as GetAssemblyFileName: the path derives from the plugin name
		//and the develop define alone, so it is stable for the whole session
		private static string GetConfigAssetPath(Registration registration)
		{
			if (!string.IsNullOrEmpty(registration.configAssetPath)) return registration.configAssetPath;

			if (registration.instance != null)
			{
				registration.configAssetPath = registration.instance.ConfigAssetPath;

				return registration.configAssetPath;
			}

			if (registration.configType == null) return string.Empty;

			AbstractPluginConfig transient = AbstractPluginConfig.CreateTransientInstance(registration.configType);
			if (transient == null) return string.Empty;

			registration.configAssetPath = transient.ConfigAssetPath;
			UnityEngine.Object.DestroyImmediate(transient);

			return registration.configAssetPath;
		}

		public static void RemoveByID(PluginID pluginID)
		{
			for (int i = registrations.Count - 1; i >= 0; i--)
			{
				if (registrations[i].id == pluginID)
				{
					registrations.RemoveAt(i);
				}
			}
		}

		//The save that SaveAsAsset postponed with "will save it once the import finishes": the asset
		//batch after that import is the first moment the folder can exist, so this is where it happens
		public static void RetryPendingSaves()
		{
			for (int i = 0; i < registrations.Count; i++)
			{
				AbstractPluginConfig instance = registrations[i].instance;
				if (instance == null) continue;
				if (AssetDatabase.Contains(instance)) continue;

				instance.SaveAsAsset();
			}

			if (staticValues.resolvedSharedConfig != null && !AssetDatabase.Contains(staticValues.resolvedSharedConfig))
			{
				staticValues.resolvedSharedConfig.SaveAsAsset();
			}
		}

		//Only the config assets: their folder can hold user content, like the Export folder of older versions or the Text Toolkit's fonts
		public static void RemoveConfigFiles()
		{
			ResolveAll();

			for (int i = 0; i < registrations.Count; i++)
			{
				AbstractPluginConfig instance = registrations[i].instance;
				if (instance == null) continue;

				//Where the asset actually sits: a relocated config does not live at its derived path
				string assetPath = AssetDatabase.GetAssetPath(instance);
				if (string.IsNullOrEmpty(assetPath))
				{
					assetPath = instance.ConfigAssetPath;
				}

				EditorUtils.DeleteAssetIfExist(assetPath);

				//Cleared, not recreated: the next thing that asks for it builds a fresh one
				registrations[i].instance = null;
			}

			if (staticValues.resolvedSharedConfig != null)
			{
				EditorUtils.DeleteAssetIfExist(staticValues.resolvedSharedConfig.AssetPath);
				staticValues.resolvedSharedConfig = null;
			}

			//The recreated configs come back with no URP answer stored, which is what makes the
			//reconcile the reset tool schedules next ask about the pipeline again
		}

		//A throwaway instance answers this: it is a plain object, nothing is read from the project
		private static string GetAssemblyFileName(Registration registration)
		{
			if (!string.IsNullOrEmpty(registration.assemblyFileName)) return registration.assemblyFileName;

			if (registration.instance != null)
			{
				registration.assemblyFileName = registration.instance.GetAssemblyNameWithExtension();

				return registration.assemblyFileName;
			}

			if (registration.configType == null) return string.Empty;

			AbstractPluginConfig transient = AbstractPluginConfig.CreateTransientInstance(registration.configType);
			if (transient == null) return string.Empty;

			registration.assemblyFileName = transient.GetAssemblyNameWithExtension();
			UnityEngine.Object.DestroyImmediate(transient);

			return registration.assemblyFileName;
		}

		private static Registration FindRegistration(PluginID id)
		{
			for (int i = 0; i < registrations.Count; i++)
			{
				if (registrations[i].id == id) return registrations[i];
			}

			return null;
		}

		private static AbstractPluginConfig Resolve(Registration registration)
		{
			if (registration == null) return null;

			if (registration.instance != null) return registration.instance;

			if (registration.configType == null) return null;

			registration.instance = LoadOrCreate(registration.configType);

			return registration.instance;
		}

		private static AbstractPluginConfig LoadOrCreate(Type configType)
		{
			AbstractPluginConfig stored = LoadStored(configType);

			if (stored != null) return stored;

			AbstractPluginConfig candidate = AbstractPluginConfig.CreateTransientInstance(configType);

			if (candidate == null) return null;

			candidate.PrepareAsNewConfig();

			return candidate;
		}

		//By path first, not by an asset database search: the path is derived from the config's own
		//script, so it answers during an import too, and a search that missed used to create a second
		//config asset. A user who moved or renamed folders has the asset somewhere the derived path is
		//not, so the search runs only when the path misses. Loads only, never creates
		private static AbstractPluginConfig LoadStored(Type configType)
		{
			AbstractPluginConfig candidate = AbstractPluginConfig.CreateTransientInstance(configType);

			if (candidate == null) return null;

			string derivedPath = candidate.ConfigAssetPath;
			UnityEngine.Object.DestroyImmediate(candidate);

			AbstractPluginConfig stored = AssetDatabase.LoadAssetAtPath<AbstractPluginConfig>(derivedPath);

			if (stored != null) return stored;

			AbstractPluginConfig relocated = FindRelocated(configType);

			if (relocated != null)
			{
				UnityEngine.Debug.LogWarning($"[AllIn1] The {relocated.GetPluginName()} configuration was found at " +
					$"'{AssetDatabase.GetAssetPath(relocated)}' instead of '{derivedPath}'. It keeps working from there.");
			}

			return relocated;
		}

		private static AbstractPluginConfig FindRelocated(Type configType)
		{
			string[] guids = AssetDatabase.FindAssets($"t:{configType.Name}");

			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				AbstractPluginConfig loaded = AssetDatabase.LoadAssetAtPath<AbstractPluginConfig>(path);

				if (loaded != null && loaded.GetType() == configType)
				{
					return loaded;
				}
			}

			return null;
		}

		private static SharedConfig ResolveSharedConfig()
		{
			SharedConfig candidate = UnityEngine.ScriptableObject.CreateInstance<SharedConfig>();

			SharedConfig stored = AssetDatabase.LoadAssetAtPath<SharedConfig>(candidate.AssetPath);

			if (stored != null)
			{
				UnityEngine.Object.DestroyImmediate(candidate);

				return stored;
			}

			candidate.SaveAsAsset();

			return candidate;
		}

#if ALLIN1_URP
		//The answer is recorded on every plugin that needs it, including the ones with no config asset yet,
		//which is why this resolves: a plugin left without it would be asked again on the next load
		public static void ConfigureURPAccepted(UniversalRendererData universalRendererData, RenderPipelineAsset renderPipelineAsset)
		{
			List<AbstractPluginConfig> plugins = ResolveAllNeedingURPConfiguration();

			for (int i = 0; i < plugins.Count; i++)
			{
				if (universalRendererData != null)
				{
					plugins[i].ConfigureRenderPipeline(universalRendererData, renderPipelineAsset);
				}

				plugins[i].ConfigureURPAccepted(renderPipelineAsset);
				AssetDatabase.SaveAssetIfDirty(plugins[i]);
			}
		}

		public static void ConfigureURPDenied(RenderPipelineAsset renderPipelineAsset)
		{
			List<AbstractPluginConfig> plugins = ResolveAllNeedingURPConfiguration();

			for (int i = 0; i < plugins.Count; i++)
			{
				plugins[i].ConfigureURPDenied(renderPipelineAsset);
				AssetDatabase.SaveAssetIfDirty(plugins[i]);
			}
		}
#endif
	}
}
