using UnityEditor;

namespace AllIn1Assets.SharedExtensions
{
	public class OutlineEffectDrawer : AbstractEffectDrawer
	{
		private MaterialProperty stencilRefMatProperty;

		public OutlineEffectDrawer(Plugin3DMaterialInspectorReferences references, 
			PropertiesConfig propertiesConfig, Plugin3DConfig pluginConfig) : base(references, propertiesConfig, pluginConfig)
		{
			this.drawerID = Constants.OUTLINE_DRAWER_ID;

			stencilRefMatProperty = FindPropertyByName("_StencilRef");
		}

		protected override void DrawProperties()
		{
			base.DrawProperties();

			if (stencilRefMatProperty != null)
			{
				EffectPropertyDrawer.DrawProperty(
					materialProperty: stencilRefMatProperty,
					labelPrefix: string.Empty,
					displayName: stencilRefMatProperty.displayName,
					customValue: string.Empty,
					allowReset: true,
					isKeywordProperty: false,
					propertyType: EffectProperty.PropertyType.BASIC,
					references: references);		
			}
		}
	}
}