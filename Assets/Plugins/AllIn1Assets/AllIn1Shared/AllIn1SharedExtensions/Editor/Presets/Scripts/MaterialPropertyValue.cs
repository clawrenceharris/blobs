using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	[System.Serializable]
	public class MaterialPropertyValue
	{
		public enum ValueType
		{
			NONE,
			INT,
			FLOAT,
			COLOR,
			TEXTURE,
		}

		public string propertyName;

		public ValueType valueType;

		public int intValue;
		public float floatValue;
		public Color colorValue;
		public Texture2D textureValue;
	}
}