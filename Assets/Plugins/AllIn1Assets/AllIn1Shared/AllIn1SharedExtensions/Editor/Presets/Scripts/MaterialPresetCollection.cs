using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	[CreateAssetMenu(menuName = "AllIn1/Shared3D/Material Preset Collection")]
	public class MaterialPresetCollection : ScriptableObject
	{
		private string[] presetNames;

		//Material Presets
		public MaterialPreset[] materialPresets;

		public MaterialPreset this[int index]
		{
			get
			{
				return materialPresets[index];
			}
		}

		public string[] GetPresetNames()
		{
			if (presetNames == null || presetNames.Length != materialPresets.Length)
			{
				presetNames = new string[materialPresets.Length];
			}

			for (int i = 0; i < materialPresets.Length; i++)
			{
				presetNames[i] = materialPresets[i].displayName;
			}

			return presetNames;
		}

		public int GetPresetIndex(MaterialPreset materialPreset)
		{
			int res = 0;

			for(int i = 0; i < materialPresets.Length; i++)
			{
				if (materialPresets[i] == materialPreset)
				{
					res = i;
					break;
				}
			}

			return res;
		}
	}
}