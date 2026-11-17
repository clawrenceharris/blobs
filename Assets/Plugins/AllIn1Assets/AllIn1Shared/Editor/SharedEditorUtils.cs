using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AllIn1Assets
{
	public static class SharedEditorUtils
	{
		public static void RemoveSymbol(string symbolToRemove)
		{
			BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
			BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

			NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
			string[] defineSymbols = new string[0];
			PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out defineSymbols);
			for (int i = defineSymbols.Length - 1; i >= 0; i--)
			{
				if (defineSymbols[i] == symbolToRemove)
				{
					ArrayUtility.RemoveAt(ref defineSymbols, i);
					break;
				}
			}

			PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defineSymbols);
		}
	}
}