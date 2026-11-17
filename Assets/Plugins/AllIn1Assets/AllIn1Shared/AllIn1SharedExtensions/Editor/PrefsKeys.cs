using UnityEditor;

namespace AllIn1Assets.SharedExtensions
{
	public class PrefsKeys
	{
		private const string LOCAL_KEY_PROPERTIES_CONFIG_CREATED_FIRST_TIME = "PropertiesConfigCreatedFirstTime";

		private Plugin3DConfig pluginConfig;


		private string keyPropertiesConfigCreatedFirstTime;

		public PrefsKeys(Plugin3DConfig pluginConfig)
		{
			this.pluginConfig = pluginConfig;

			keyPropertiesConfigCreatedFirstTime = $"{pluginConfig.GetPluginName()}_{LOCAL_KEY_PROPERTIES_CONFIG_CREATED_FIRST_TIME}";
		}

		public bool PropertiesConfigCreatedFirstTime
		{
			get
			{
				return SessionState.GetBool(keyPropertiesConfigCreatedFirstTime, false);
			}
			set
			{
				SessionState.SetBool(keyPropertiesConfigCreatedFirstTime, value);
			}
		}
	}
}