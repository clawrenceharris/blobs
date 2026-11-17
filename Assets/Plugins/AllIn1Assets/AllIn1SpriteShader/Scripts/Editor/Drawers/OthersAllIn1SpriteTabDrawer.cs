
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.AllIn1SpriteShader
{
	public class OthersAllIn1SpriteTabDrawer : AssetWindowTabDrawer
	{
		private const string TAB_NAME = "Others";
		private const float BUTTON_WIDTH = 600f;
		private AllIn1SpriteShaderConfig pluginConfig;
		private AllIn1ShaderWindow.ImageType imageType;

		public AllIn1Shader.ShaderTypes AllIn1DefaultShader
		{
			get { return AllIn1SpriteShaderConfig.DefaultShaderType; }
			set { AllIn1SpriteShaderConfig.DefaultShaderType = value; }
		}

		public OthersAllIn1SpriteTabDrawer(CommonStyles commonStyles, AllIn1CommonWindow parentWindow, AbstractPluginConfig abstractPluginConfig) : base(commonStyles, parentWindow, abstractPluginConfig)
		{
			pluginConfig = (AllIn1SpriteShaderConfig)abstractPluginConfig;
		}

		public override void Draw()
		{
			GUILayout.Label("Asset Image Display Options", commonStyles.bigLabel);
			GUILayout.Space(20);

			int previousImageType = (int)imageType;

			imageType = (AllIn1ShaderWindow.ImageType)EditorPrefs.GetInt("allIn1ImageConfig", 0);
			imageType = (AllIn1ShaderWindow.ImageType)EditorGUILayout.EnumPopup(imageType, GUILayout.MaxWidth(200));
			EditorPrefs.SetInt("allIn1ImageConfig", (int)imageType);

			EditorUtils.DrawThinLine();

			DefaultAssetShader();

			EditorUtils.DrawThinLine();
			GUILayout.Space(10);
			SceneNotificationsToggle();

			EditorUtils.DrawThinLine();
			GUILayout.Space(10);
			RefreshLitShader();
		}

		private static void SceneNotificationsToggle()
		{
			float previousLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 200f;
			bool areNotificationsEnabled = EditorPrefs.GetInt("DisplaySceneViewNotifications", 1) == 1;
			areNotificationsEnabled = EditorGUILayout.Toggle("Display Scene View Notifications", areNotificationsEnabled);
			EditorPrefs.SetInt("DisplaySceneViewNotifications", areNotificationsEnabled ? 1 : 0);
			EditorGUIUtility.labelWidth = previousLabelWidth;
		}

		private static void RefreshLitShader()
		{
			GUILayout.Label("Force the Lit Shader to be reconfigured");
			GUILayout.Label("If you are getting some error or have changed the render pipeline press the button below");
			if (GUILayout.Button("Refresh Lit Shader", GUILayout.MaxWidth(BUTTON_WIDTH)))
			{
				AllIn1ShaderImporter.ForceReimport();
			}
		}

		private void DefaultAssetShader()
		{
			GUILayout.Label("Default Asset Shader", commonStyles.bigLabel);
			GUILayout.Space(20);
			GUILayout.Label("This is the shader variant that will be assigned by default to Sprites and UI Images when the asset component is added", EditorStyles.boldLabel);

			bool isUrp = false;
			Shader temp = Shader.Find("AllIn1SpriteShader/AllIn1Urp2dRenderer");
			if (temp != null) isUrp = true;

			int previousShaderType = (int)AllIn1DefaultShader;
			//AllIn1Shader.ShaderTypes shaderTypes = (AllIn1Shader.ShaderTypes)PlayerPrefs.GetInt("allIn1DefaultShader", (int)AllIn1Shader.ShaderTypes.Default);

			AllIn1Shader.ShaderTypes shaderTypes = (AllIn1Shader.ShaderTypes)EditorGUILayout.EnumPopup(AllIn1DefaultShader, GUILayout.MaxWidth(200));
			//AllIn1DefaultShader = (AllIn1Shader.ShaderTypes)EditorGUILayout.EnumPopup(AllIn1DefaultShader, GUILayout.MaxWidth(200));

			bool showUrpWarning = false;
			double warningTime = 0;

			if (previousShaderType != (int)shaderTypes)
			{
				if (!isUrp && shaderTypes == AllIn1Shader.ShaderTypes.Urp2dRenderer)
				{
					showUrpWarning = true;
					warningTime = EditorApplication.timeSinceStartup + 5;
				}
				else
				{
					AllIn1DefaultShader = shaderTypes;
					showUrpWarning = false;
				}
			}

			if (warningTime < EditorApplication.timeSinceStartup) showUrpWarning = false;
			if (isUrp) showUrpWarning = false;
			if (!isUrp && !showUrpWarning && shaderTypes == AllIn1Shader.ShaderTypes.Urp2dRenderer)
			{
				showUrpWarning = true;
				warningTime = EditorApplication.timeSinceStartup + 5;
				shaderTypes = AllIn1Shader.ShaderTypes.Default;
				AllIn1DefaultShader = shaderTypes;
			}

			if (showUrpWarning) EditorGUILayout.HelpBox(
				"You can't set the URP 2D Renderer variant since you didn't import the URP package available in the asset root folder (SEE DOCUMENTATION)",
				MessageType.Error,
				true);
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