using AllIn1Assets;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace AllIn1Assets.SharedExtensions
{
	public class URPSettingsUserPref : ScriptableObject
	{
		public const string ASSET_NAME = "URPSettingsUserPref";

		public URPFeatureUserPref[] preferences;

		public static URPSettingsUserPref CreateAsset(Plugin3DConfig pluginConfig)
		{
			URPSettingsUserPref res = ScriptableObject.CreateInstance<URPSettingsUserPref>();
			res.Init(pluginConfig.urpSettings);

			AssetDatabase.CreateAsset(res, pluginConfig.URPSettingsUserPrefAssetPath);

			return res;
		}

		public void Init(URPSettings urpSettings)
		{
			preferences = new URPFeatureUserPref[urpSettings.configs.Length];

			for(int i = 0; i < urpSettings.configs.Length; i++)
			{
				preferences[i] = new URPFeatureUserPref(urpSettings.configs[i]);
			}
		}

		public URPFeatureUserPref FindPreferenceByID(string id)
		{
			URPFeatureUserPref res = null;

			for(int i = 0; i < preferences.Length; i++)
			{
				if (preferences[i].id == id)
				{
					res = preferences[i];
					break;
				}
			}

			return res;
		}
	}
}