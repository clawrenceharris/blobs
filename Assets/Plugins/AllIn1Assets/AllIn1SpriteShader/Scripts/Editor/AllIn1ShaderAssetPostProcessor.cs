#if UNITY_EDITOR
using System;
using UnityEditor;

namespace AllIn1Assets.AllIn1SpriteShader
{
	public class AllIn1ShaderAssetPostProcessor : AssetPostprocessor
	{
		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
		{
			if (AllIn1ShaderImporter.IsWritingLitShader)
				return;

			if (!NeedsLitShaderRefresh(importedAssets) && !NeedsLitShaderRefresh(movedAssets))
				return;

			//Erased now, so a domain reload before the delayCall still regenerates the Lit shaders
			AllIn1ShaderImporter.InvalidateSession();
			EditorApplication.delayCall -= AllIn1ShaderImporter.ForceReimport;
			EditorApplication.delayCall += AllIn1ShaderImporter.ForceReimport;
		}

		private static bool NeedsLitShaderRefresh(string[] assetPaths)
		{
			for (int i = 0; i < assetPaths.Length; i++)
			{
				if (assetPaths[i].EndsWith(Constants.MAIN_ASSEMBLY_NAME, StringComparison.Ordinal) || AllIn1ShaderImporter.IsLitShaderFile(assetPaths[i]))
					return true;
			}

			return false;
		}
	}
}
#endif
