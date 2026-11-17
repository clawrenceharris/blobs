using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class BlendingPresetCollection : ScriptableObject
	{
		public BlendingMode this[int key]
		{
			get => presets[key];
		}

		public BlendingMode[] presets;

		public string[] CreateStringsArray()
		{
			string[] res = new string[presets.Length];

			for(int i = 0; i < presets.Length; i++)
			{
				res[i] = presets[i].displayName;
			}

			return res;
		}

		public int GetIndex(BlendingMode materialPreset)
		{
			int res = -1;

			for(int i = 0; i < presets.Length; i++)
			{
				if (presets[i] == materialPreset)
				{
					res = i;
					break;
				}
			}

			return res;
		}
	}
}