using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class OtherTabDrawer : AssetWindowTabDrawer
	{
		private const string TAB_NAME = "Other";

		protected Plugin3DConfig pluginConfig;

		public OtherTabDrawer(CommonStyles commonStyles, AllIn1CommonWindow parentWindow, Plugin3DConfig pluginConfig) : base(commonStyles, parentWindow, pluginConfig)
		{
			this.pluginConfig = pluginConfig;
		}

		public override void Show()
		{

		}

		public override void Hide()
		{

		}

		public override void OnEnable()
		{

		}

		public override void OnDisable()
		{

		}

		public override void EnteredPlayMode()
		{

		}

		public override void Draw()
		{
			if (HasDefaultMaterialChoice())
			{
				DrawDefaultMaterials();
			}

			EditorGUILayout.Space();
			if (GUILayout.Button("Refresh the Material Inspector Properties Configuration"))
			{
				pluginConfig.shadersCreatorTool.BuildShaderFiles(pluginConfig);
				PropertiesConfigCreator.CreateConfig(pluginConfig);
			}

			EditorGUILayout.LabelField("The asset uses auto generated cached data to display the properties of the Material inspector\nYou should never need this button", commonStyles.wordWrappedStyle);
		}

		private bool HasDefaultMaterialChoice()
		{
			bool res = pluginConfig.materialPresetCollection.materialPresets.Length > 1 || pluginConfig.defaultPreset.materialModifiable;
			return res;
		}

		private void DrawDefaultMaterials()
		{
			GUILayout.Label("Default Materials", commonStyles.bigLabel);
			GUILayout.Space(20);

			EditorGUI.BeginChangeCheck();
			pluginConfig.defaultPreset = Shader3DSharedUtils.DrawMaterialPresetSelector("Material Preset", pluginConfig.defaultPreset, pluginConfig.materialPresetCollection);
			bool presetChanged = EditorGUI.EndChangeCheck();

			EditorGUI.BeginDisabledGroup(!pluginConfig.defaultPreset.materialModifiable);
			EditorGUI.BeginChangeCheck();
			pluginConfig.defaultPreset.material = (Material)EditorGUILayout.ObjectField("Material", pluginConfig.defaultPreset.material, typeof(Material), false);
			bool customPresetChanged = EditorGUI.EndChangeCheck();
			EditorGUI.EndDisabledGroup();

			if (presetChanged || customPresetChanged)
			{
				EditorUtility.SetDirty(pluginConfig);
				EditorUtility.SetDirty(pluginConfig.defaultPreset);
			}

			//if (projectTypeChanged)
			//{
			//	pluginConfig.RefreshDefaultMaterial();
			//}

			/*
			if(projectTypeChanged || defaultPresetChanged)
			{
				pluginConfig.Save();
			}
			*/


			EditorGUILayout.Space();
			EditorUtils.DrawThinLine();
			EditorGUILayout.Space();
		}

		public override string GetTabName()
		{
			return TAB_NAME;
		}
	}
}