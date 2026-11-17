using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class URPSettingsDrawer : AssetWindowTabDrawer
	{
		private const string TAB_NAME = "URP Settings";

		private Plugin3DConfig plugin3DConfig;

		private URPSettings urpSettings;
		private URPSettingsUserPref urpSettingsUserPref;
		
		public URPSettingsDrawer(CommonStyles commonStyles, AllIn1Asset3DWindow parentWindow, Plugin3DConfig plugin3DConfig) : base(commonStyles, parentWindow, plugin3DConfig)
		{
			this.plugin3DConfig = plugin3DConfig;
			this.urpSettings = plugin3DConfig.urpSettings;
			this.urpSettingsUserPref = plugin3DConfig.urpSettingsUserPref;
		}

		public override void Draw()
		{
			GUILayout.Label("Shader Feature Configuration", commonStyles.bigLabel);
			EditorGUILayout.HelpBox("Enable/disable shader features to optimize compilation time and editor performance. \n" +
			                        "Hover each feature to get a more detailed tooltip.", MessageType.Info);
			
			GUILayout.Space(5);

			for(int i = 0; i < urpSettings.configs.Length; i++)
			{
				EditorGUILayout.BeginHorizontal();

				URPFeatureConfig urpFeatureConfig = urpSettings.configs[i];
				URPFeatureUserPref urpFeatureUserPref = urpSettingsUserPref.FindPreferenceByID(urpFeatureConfig.shaderDefine);

				GUIContent toggleContent = new GUIContent(urpFeatureConfig.displayName, urpFeatureConfig.tooltip);
				urpFeatureUserPref.enabled = EditorGUILayout.ToggleLeft(toggleContent, urpFeatureUserPref.enabled);
				
				EditorGUILayout.EndHorizontal();
			}
			
			GUILayout.Space(10);
			
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("Apply Changes"))
			{
				ApplyFeatureChanges();
			}
			
			if(GUILayout.Button("Reset to Defaults"))
			{
				ResetToDefaults();
			}
			EditorGUILayout.EndHorizontal();
			
			GUILayout.Space(20);
			EditorUtils.DrawThinLine();
			
			GUILayout.Label("Configure AllIn13D to work correctly with URP", commonStyles.bigLabel);
			if (GUILayout.Button("Configure"))
			{
#if ALLIN1_URP
				URPConfigurator.Configure(forceConfigure: true);
#endif
			}
		}

		public override void Hide()
		{

		}

		public override void Show()
		{
		
		}

		public override void OnDisable()
		{
		
		}

		public override void OnEnable()
		{
		
		}

		public override void EnteredPlayMode()
		{
		
		}

		private void ApplyFeatureChanges()
		{
			string urpDefinesFilePath = plugin3DConfig.FeturesURPDefinesFilesPath;
			string hlslLibraryTemplatePath = plugin3DConfig.HLSLTemplateFilePath;
			AllIn1Logger.Log(hlslLibraryTemplatePath);

			if (!File.Exists(urpDefinesFilePath))
			{
				AllIn1Logger.LogError("URP defines file not found");
				return;
			}

			if (!File.Exists(hlslLibraryTemplatePath))
			{
				AllIn1Logger.LogError("HLSL library template not found");
				return;
			}

			string fileText = File.ReadAllText(hlslLibraryTemplatePath);
			string content = string.Empty;
			for(int i = 0; i < urpSettingsUserPref.preferences.Length; i++)
			{
				if (urpSettingsUserPref.preferences[i].enabled)
				{
					string line = string.Format(Constants.DEFINE_LINE_FORMAT, urpSettings.configs[i].shaderDefine);

					content += line;
					content += Environment.NewLine;
				}
			}

			fileText = fileText.Replace("<LIBRARY_DEFINE>", "ALLIN13DSHADER_FEATURESURP_DEFINES"); 
			fileText = fileText.Replace("<CONTENT>", content);

			File.WriteAllText(urpDefinesFilePath, fileText);

			EditorUtility.SetDirty(urpSettingsUserPref);
			AssetDatabase.Refresh();
		}

		private void ResetToDefaults()
		{

			for(int i = 0; i < urpSettingsUserPref.preferences.Length; i++)
			{
				URPFeatureUserPref preference = urpSettingsUserPref.preferences[i];
				URPFeatureConfig urpFeatureConfig = urpSettings.FindConfigByID(preference.id);

				urpSettingsUserPref.preferences[i].Init(urpFeatureConfig);
			}
		}

		public override string GetTabName()
		{
			return TAB_NAME;
		}
	}
}