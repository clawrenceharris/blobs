using UnityEditor;
using UnityEngine;

namespace AllIn1Assets
{
	public static class ConfigureAllTool
	{
		[MenuItem("Tools/AllIn1/Configuration/Reset All Plugin Configuration", priority = 20)]
		public static void ResetAllConfiguration()
		{
			bool confirmed = EditorUtility.DisplayDialog(
				"Reset All Plugin Configuration",
				"This resets every AllIn1 plugin to its just imported state. Save paths go back to their defaults and " +
				"first time setup runs again.\n\nMaterials, textures and anything else you generated are kept.",
				"Reset", "Cancel");

			if (confirmed)
			{
				CloseOpenWindows();
				PluginRegistry.RemoveConfigFiles();

#if ALLIN1_URP
				//The recreated configs carry no URP answer. Asked now instead of on the next reload
				EditorApplication.delayCall += URPConfigurator.Reconcile;
#endif
			}
		}

		//Their tab drawers cache the config that RemoveConfigFiles is about to destroy
		private static void CloseOpenWindows()
		{
			AllIn1CommonWindow[] windows = Resources.FindObjectsOfTypeAll<AllIn1CommonWindow>();

			for (int i = 0; i < windows.Length; i++)
			{
				windows[i].Close();
			}
		}
	}
}
