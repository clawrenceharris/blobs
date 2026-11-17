using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.AllIn1SpriteShader
{
	public class SavePathsAllIn1SpriteTabDrawer : AssetWindowTabDrawer
	{
		private const string TAB_NAME = "Save Paths";

		private AllIn1SpriteShaderConfig pluginConfig;

		public SavePathsAllIn1SpriteTabDrawer(CommonStyles commonStyles, AllIn1CommonWindow parentWindow, AbstractPluginConfig abstractPluginConfig) : base(commonStyles, parentWindow, abstractPluginConfig)
		{
			pluginConfig = (AllIn1SpriteShaderConfig)abstractPluginConfig;
		}

		public override void Draw()
		{
			GUILayout.Label("Material Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);
			GUILayout.Label("Select the folder where new Materials will be saved when the Save Material To Folder button of the asset component is pressed", EditorStyles.boldLabel);
			pluginConfig.MaterialSavePath = EditorUtils.DrawSelectorFolder(pluginConfig.MaterialSavePath, "New Material Folder");

			EditorUtils.DrawThinLine();
			GUILayout.Label("Render Material to Image Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);
			EditorGUILayout.BeginHorizontal();
			{
				float scaleSlider = PlayerPrefs.GetFloat(AllIn1Shader.PREF_RENDER_IMAGES_SCALE, 1f);
				GUILayout.Label("Rendered Image Texture Scale", GUILayout.MaxWidth(190));
				float newScale = EditorGUILayout.Slider(scaleSlider, 0.2f, 5f, GUILayout.MaxWidth(200));
				if (GUILayout.Button("Default Value", GUILayout.MaxWidth(100))) newScale = 1f;
				if (!Mathf.Approximately(newScale, scaleSlider)) PlayerPrefs.SetFloat(AllIn1Shader.PREF_RENDER_IMAGES_SCALE, newScale);
			}
			EditorGUILayout.EndHorizontal();
			GUILayout.Label("Select the folder where new Images will be saved when the Render Material To Image button of the asset component is pressed", EditorStyles.boldLabel);
			pluginConfig.RenderImageSavePath = EditorUtils.DrawSelectorFolder(pluginConfig.RenderImageSavePath, "New Render Image Folder");


		}

		public override void EnteredPlayMode()
		{

		}

		public override string GetTabName()
		{
			return TAB_NAME;
		}

		public override void Hide()
		{
		
		}

		public override void OnDisable()
		{
		
		}

		public override void OnEnable()
		{
		
		}

		public override void Show()
		{
		
		}
	}
}