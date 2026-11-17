using UnityEngine;

namespace AllIn1Assets
{
	[ExecuteInEditMode]
	public class DemoCameraController : MonoBehaviour
	{
		public Camera cam;

		private void Awake()
		{
#if ALLIN1_BIRP
			cam.depthTextureMode = DepthTextureMode.Depth;
#endif
		}
	}
}