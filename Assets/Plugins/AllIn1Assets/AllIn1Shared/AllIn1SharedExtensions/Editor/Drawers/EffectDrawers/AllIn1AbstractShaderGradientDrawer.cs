using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public abstract class AllIn1AbstractShaderGradientDrawer : MaterialPropertyDrawer
	{
		private GradientEditorDrawer gradientEditorDrawer;

		private void RefreshReferences(MaterialProperty prop)
		{
			if(gradientEditorDrawer == null)
			{
				gradientEditorDrawer = new GradientEditorDrawer();
				gradientEditorDrawer.SetPlugin(GetPluginConfig());
			}
		} 

		public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			RefreshReferences(prop); 

			Texture texValue = prop.textureValue;

			EditorGUI.BeginChangeCheck();
			Texture newTex = gradientEditorDrawer.Draw(position, texValue, label.text);
			if (EditorGUI.EndChangeCheck())
			{
				prop.textureValue = newTex;
			}
		}

		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			float res = 0f;
			if(gradientEditorDrawer != null)
			{
				res = gradientEditorDrawer.GetPropertyHeight();
			}

			return res;
		}

		public abstract Plugin3DConfig GetPluginConfig();
	}
}