using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class EffectToggleOverride : AbstractEffectOverride
	{
		public bool boolValue;

		public EffectToggleOverride(AllIn13DEffectConfig effectConfig) : base(effectConfig)
		{

		}

		protected override void ApplyMainPropertyChanges(Material mat, Plugin3DConfig pluginConfig)
		{
			if (boolValue)
			{
				mat.EnableKeyword(keywords[0].keyword);
				mat.SetFloat(propertyName, 1f);
			}
			else
			{
				mat.DisableKeyword(keywords[0].keyword);
				mat.SetFloat(propertyName, 0f);
			}

			pluginConfig.shaderResolver.ResolveShader(mat, pluginConfig);
		}
	}
}