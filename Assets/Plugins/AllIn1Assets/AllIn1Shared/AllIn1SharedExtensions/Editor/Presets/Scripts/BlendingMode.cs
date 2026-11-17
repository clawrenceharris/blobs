using UnityEngine;
using UnityEngine.Rendering;

namespace AllIn1Assets.SharedExtensions
{
	public class BlendingMode : ScriptableObject
	{
		public string displayName;

		public RenderQueue renderQueue;
		public UnityEngine.Rendering.BlendMode blendSrc;
		public UnityEngine.Rendering.BlendMode blendDst;
		public bool depthWrite;

		public bool isTransparent;

		public string[] defaultEnabledEffects;
	}
}