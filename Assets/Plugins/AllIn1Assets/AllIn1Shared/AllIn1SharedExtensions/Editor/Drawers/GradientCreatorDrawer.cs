using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class GradientCreatorDrawer
	{
		private GradientCreatorTool gradientCreatorTool;
		private CommonStyles commonStyles;

		private Plugin3DConfig pluginConfig;

		private Texture2D GradientTex
		{
			get
			{
				return gradientCreatorTool.gradientTex;
			}
			set
			{
				gradientCreatorTool.gradientTex = value;
			}
		}

		private Gradient Grad
		{
			get
			{
				return gradientCreatorTool.gradient;
			}
			set
			{
				gradientCreatorTool.gradient = value;
			}
		}

		private TextureSizes GradientSizes
		{
			get	
			{
				return gradientCreatorTool.gradientSizes;
			}
			set
			{
				gradientCreatorTool.gradientSizes = value;
			}
		}

		private FilterMode GradientFiltering
		{
			get
			{
				return gradientCreatorTool.gradientFiltering;
			}
			set
			{
				gradientCreatorTool.gradientFiltering = value;
			}
		}

		public GradientCreatorDrawer(GradientCreatorTool gradientCreatorTool, CommonStyles commonStyles, Plugin3DConfig pluginConfig)
		{
			this.gradientCreatorTool = gradientCreatorTool;
			this.commonStyles = commonStyles;
			this.pluginConfig = pluginConfig;
		}

		public void Draw()
		{
			GUILayout.Label("Color Gradient Creator", commonStyles.bigLabel);
			GUILayout.Space(20);
			GUILayout.Label("This feature can be used to create textures for the Color Ramp Effect", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			Grad = EditorGUILayout.GradientField("Color Gradient: ", Grad, GUILayout.Height(25), GUILayout.MaxWidth(CommonStyles.BUTTON_WIDTH));
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label("Texture Size:", GUILayout.MaxWidth(145));
				GradientSizes = (TextureSizes)EditorGUILayout.EnumPopup(GradientSizes, GUILayout.MaxWidth(200));
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label("New Textures Filtering: ", GUILayout.MaxWidth(145));
				GradientFiltering = (FilterMode)EditorGUILayout.EnumPopup(GradientFiltering, GUILayout.MaxWidth(200));
			}
			EditorGUILayout.EndHorizontal();
			
			bool gradientChanged = EditorGUI.EndChangeCheck();
			if (gradientChanged)
			{
				gradientCreatorTool.CreateGradientTexture();
			}

			GUILayout.Space(20);
			GUILayout.Label("Select the folder where new Color Gradient Textures will be saved", EditorStyles.boldLabel);

			pluginConfig.GradientSavePath = EditorUtils.DrawSelectorFolder(pluginConfig.GradientSavePath, "Gradients");
			if (Directory.Exists(pluginConfig.GradientSavePath))
			{
				if (GUILayout.Button("Save Color Gradient Texture", GUILayout.MaxWidth(CommonStyles.BUTTON_WIDTH)))
				{
					EditorUtils.SaveTextureAsPNG(pluginConfig.GradientSavePath, "ColorGradient", "Gradient", GradientTex, GradientFiltering, 
						TextureImporterType.Default, TextureWrapMode.Clamp);
				}
			}
		}
	}
}