using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class EffectEnumOverride : AbstractEffectOverride
	{
		public string[] enumOptions;

		public int index;

		public EffectEnumOverride(AllIn13DEffectConfig effectConfig) : base(effectConfig)
		{
			enumOptions = new string[effectConfig.keywords.Count];
			for(int i = 0; i < enumOptions.Length; i++)
			{
				enumOptions[i] = effectConfig.keywords[i].displayName;
			}
		}

		protected override void ApplyMainPropertyChanges(Material mat, Plugin3DConfig pluginConfig)
		{
			for(int i = 0; i < keywords.Count; i++)
			{
				mat.DisableKeyword(keywords[i].keyword);
			}

			mat.EnableKeyword(keywords[index].keyword);
			mat.SetFloat(propertyName, (float)index);

			pluginConfig.shaderResolver.ResolveShader(mat, pluginConfig);
		}
	}
}