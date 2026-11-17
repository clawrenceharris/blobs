using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public abstract class PropertiesConfigCollection : ScriptableObject
	{
		//public PropertiesConfig propertiesConfig;

		public abstract PropertiesConfig GetPropertiesConfig();

		public abstract void AddConfig(PropertiesConfig config);

		//public void AddConfig(PropertiesConfig config)
		//{
		//	this.propertiesConfig = config;
		//}

		public abstract PropertiesConfig FindPropertiesConfigByShader(Shader shader);

		//public PropertiesConfig FindPropertiesConfigByShader(Shader shader)
		//{
		//	PropertiesConfig res = null;

		//	res = propertiesConfig;

		//	return res;
		//}


		public abstract bool IsAllIn3DShaderMaterial();
		/*
		public bool IsAllIn3DShaderMaterial(Material mat)
		{
			bool res = false;

			res = mat.shader.name.Contains("AllIn13DShader");

			return res;
		}
		*/
	}
}