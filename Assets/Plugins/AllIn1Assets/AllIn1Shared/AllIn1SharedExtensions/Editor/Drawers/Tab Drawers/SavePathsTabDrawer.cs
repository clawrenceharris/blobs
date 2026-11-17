using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class SavePathsTabDrawer : AssetWindowTabDrawer
	{
		private const string TAB_NAME = "Save Paths";

		protected Plugin3DConfig plugin3DConfig;
		// Off for an asset with no Render Material to Image button
		private readonly bool showRenderImagePath;

		public SavePathsTabDrawer(CommonStyles commonStyles, AllIn1Asset3DWindow parentWindow, Plugin3DConfig pluginConfig, bool showRenderImagePath = true) : base(commonStyles, parentWindow, pluginConfig)
		{
			this.plugin3DConfig = pluginConfig;
			this.showRenderImagePath = showRenderImagePath;
		}

		public override void OnEnable()
		{

		}

		public override void OnDisable()
		{

		}

		public override void Show()
		{

		}

		public override void Hide()
		{

		}

		public override void EnteredPlayMode()
		{

		}

		public override void Draw()
		{
			GUILayout.Label("Material Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);
			GUILayout.Label("Select the folder where new Materials will be saved when the Save Material To Folder button of the asset component is pressed", EditorStyles.boldLabel);
			plugin3DConfig.MaterialSavePath = EditorUtils.DrawSelectorFolder(plugin3DConfig.MaterialSavePath, "New Material Folder");

			if (!showRenderImagePath) { return; }

			EditorUtils.DrawThinLine();
			GUILayout.Label("Render Material to Image Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Rendered Image Texture Scale", GUILayout.MaxWidth(190));
			plugin3DConfig.RenderImageScale = EditorGUILayout.Slider(plugin3DConfig.RenderImageScale, 0.2f, 5f, GUILayout.MaxWidth(200));
			if (GUILayout.Button("Default Value", GUILayout.MaxWidth(100)))
			{
				plugin3DConfig.RenderImageScale = 1f;
			}
			EditorGUILayout.EndHorizontal();

			plugin3DConfig.RenderImageSavePath = EditorUtils.DrawSelectorFolder(plugin3DConfig.RenderImageSavePath, "New Images Folder");

			/*
			GUILayout.Label("Baked Shader Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);
			GUILayout.Label("Select the folder where baked shaders will be saved when the Bake Shader Keywords button of the material inspector is pressed", EditorStyles.boldLabel);
			pluginConfig.BakedShaderSavePath = EditorUtils.DrawSelectorFolder(pluginConfig.BakedShaderSavePath, "New Bake Shader Folder");
			*/
		}

		public override string GetTabName()
		{
			return TAB_NAME;
		}
	}
}