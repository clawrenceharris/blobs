using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class PropertiesConfigCreator
	{
		public static PropertiesConfigCollection CreateConfig(Plugin3DConfig pluginConfig)
		{
			PropertiesConfigCollection propertiesCollection = CreatePropertiesCollection(pluginConfig);
			EffectGroupGlobalConfigCollection effectGroupGlobalConfigCollection = GetEffectGroupGlobalConfigCollection();
			EffectsExtraData effectsExtraData = pluginConfig.effectsExtraData;
			
			CreatePropertiesConfig(pluginConfig, effectGroupGlobalConfigCollection, effectsExtraData, propertiesCollection, pluginConfig.GetMainShaderName());
			
			EditorUtility.SetDirty(propertiesCollection);
			AssetDatabase.SaveAssets();
			
			EditorPrefs.SetFloat(pluginConfig.lastTimeShaderPropertiesRebuiltKey, (float)EditorApplication.timeSinceStartup);

			AllIn1Logger.LogWarning("Creating data...");

			return propertiesCollection;
		}

		private static PropertiesConfigCollection CreatePropertiesCollection(Plugin3DConfig pluginConfig)
		{
			PropertiesConfigCollection res = pluginConfig.CreatePropertiesConfigSO();
			AssetDatabase.CreateAsset(res, pluginConfig.PropertiesConfigCollectionAssetPath);

			return res;
		}

		private static EffectGroupGlobalConfigCollection GetEffectGroupGlobalConfigCollection()
		{
			EffectGroupGlobalConfigCollection res = EditorUtils.FindAsset<EffectGroupGlobalConfigCollection>();
			return res;
		}

		private static void CreatePropertiesConfig(Plugin3DConfig pluginConfig, EffectGroupGlobalConfigCollection effectGroupGlobalConfigCollection, 
			EffectsExtraData effectsExtraData, PropertiesConfigCollection propertiesCollection, string shaderName)
		{
			string shaderNameWithExtension = shaderName + ".shader";
			string assetPath = EditorUtils.CombineAssetPath(pluginConfig.GenericShadersFolderPath, shaderNameWithExtension);

			Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);

			PropertiesConfig propertiesConfig = pluginConfig.CreatePropertiesConfig();
			propertiesConfig.shader = shader;

			propertiesConfig.CreateConfig(effectGroupGlobalConfigCollection, effectsExtraData);

			propertiesCollection.AddConfig(propertiesConfig);
		}
	}
}