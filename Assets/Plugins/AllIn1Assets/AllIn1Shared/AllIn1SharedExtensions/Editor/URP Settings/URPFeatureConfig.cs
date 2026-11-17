using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	[System.Serializable]
	public class URPFeatureConfig
	{
		public string shaderDefine;
		public bool defaultValue;
		[TextArea] public string displayName;
		[TextArea] public string tooltip;
	}
}