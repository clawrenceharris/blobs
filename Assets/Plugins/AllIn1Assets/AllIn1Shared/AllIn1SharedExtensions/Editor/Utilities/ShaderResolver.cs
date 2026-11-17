using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class ShaderResolver
	{
		public virtual bool ResolveShader(Material targetMat, Plugin3DConfig pluginConfig)
		{
			return false;
		}
	}
}