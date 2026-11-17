using UnityEditor;

namespace AllIn1Assets.SharedExtensions
{
	public class ColorRampEffectDrawer : AbstractEffectDrawer
	{
		public ColorRampEffectDrawer(Plugin3DMaterialInspectorReferences references, 
			PropertiesConfig propertiesConfig, Plugin3DConfig pluginConfig) : base(references, propertiesConfig, pluginConfig)
		{
			this.drawerID = Constants.COLOR_RAMP_EFFECT_DRAWER_ID;
		}

		protected override void DrawProperties()
		{
			for(int i = 0; i < effectConfig.effectProperties.Count; i++)
			{
				DrawProperty(effectConfig.effectProperties[i]);
				if (i == 2)
				{
					EditorGUILayout.LabelField("*Set the Color Ramp Texture to Repeat Wrap Mode if using Tiling and/or Scroll Speed", references.smallLabelStyle);
				}
			}
		}
	}
}