using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class MaterialPreset : ScriptableObject
	{
		public string id;
		public string displayName;
		public Material material;
		public bool materialModifiable;
	}
}