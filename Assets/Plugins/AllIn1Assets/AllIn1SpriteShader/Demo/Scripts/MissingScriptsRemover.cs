using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AllIn1Assets.AllIn1SpriteShader
{
	public class MissingScriptsRemover : MonoBehaviour
	{
		private void Awake()
		{
#if UNITY_EDITOR
			GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
#endif
		}
	}
}