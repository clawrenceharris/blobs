namespace AllIn1Assets.SharedExtensions
{
	public class GeneralEffectDrawer : AbstractEffectDrawer
	{
		public GeneralEffectDrawer(Plugin3DMaterialInspectorReferences references, 
			PropertiesConfig propertiesConfig, Plugin3DConfig pluginConfig) : base(references, propertiesConfig, pluginConfig)
		{
			this.drawerID = Constants.GENERAL_EFFECT_DRAWER_ID;
		}
	}
}