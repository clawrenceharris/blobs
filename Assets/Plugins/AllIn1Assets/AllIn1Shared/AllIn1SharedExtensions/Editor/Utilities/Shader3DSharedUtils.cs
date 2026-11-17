using System.Linq;
using UnityEditor;

namespace AllIn1Assets.SharedExtensions
{
	public static class Shader3DSharedUtils
	{
		public static MaterialPreset DrawMaterialPresetSelector(string label, MaterialPreset selectedPreset, MaterialPresetCollection materialPresetCollection)
		{
			MaterialPreset res = null;

			int selectedIndex = materialPresetCollection.GetPresetIndex(selectedPreset);
			selectedIndex = EditorGUILayout.Popup(label, selectedIndex, materialPresetCollection.GetPresetNames());

			res = materialPresetCollection[selectedIndex];
			return res;
		}

		public static bool NeedsToRecreateAsset(string assetPath, string[] importedAssets, string[] deletedAssets)
		{
			bool res = importedAssets.Contains(assetPath) || deletedAssets.Contains(assetPath);
			return res;
		}
	}
}