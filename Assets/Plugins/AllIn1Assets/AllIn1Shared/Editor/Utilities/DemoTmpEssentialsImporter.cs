using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AllIn1Assets
{
	//Opening a demo scene of the suite without TMP Essentials imports them silently, then reopens the scene so its texts pick them up
	[InitializeOnLoad]
	public static class DemoTmpEssentialsImporter
	{
		private const string ESSENTIALS_PACKAGE_NAME = "TMP Essential Resources";
		private const string SUITE_FOLDER = "/AllIn1Assets/";

		private static string pendingScenePath;

		static DemoTmpEssentialsImporter()
		{
			EditorSceneManager.sceneOpened += OnSceneOpened;
		}

		private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
		{
			if (Application.isBatchMode || pendingScenePath != null || mode != OpenSceneMode.Single) return;
			if (!scene.path.Replace('\\', '/').Contains(SUITE_FOLDER)) return;
			if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;

			//Looked up by name: the TMP assembly is named differently in 2022.3 and Unity 6
			Type importer = AppDomain.CurrentDomain.GetAssemblies()
				.Select(a => a.GetType("TMPro.TMP_PackageResourceImporter"))
				.FirstOrDefault(t => t != null);
			if (importer == null) return;

			pendingScenePath = scene.path;
			AssetDatabase.importPackageCompleted += OnImportCompleted;
			AssetDatabase.importPackageFailed += OnImportFailed;
			importer.GetMethod("ImportResources").Invoke(null, new object[] { true, false, false });
		}

		private static void OnImportCompleted(string packageName)
		{
			if (packageName != ESSENTIALS_PACKAGE_NAME) return;

			string scenePath = pendingScenePath;
			StopListening();
			CloseTmpImporterWindows();

			EditorApplication.delayCall += () =>
			{
				Scene active = SceneManager.GetActiveScene();
				if (active.path == scenePath && !active.isDirty) EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
				//TMP can open its importer window after the import already finished, while the scene's texts load
				EditorApplication.delayCall += CloseTmpImporterWindows;
			};
		}

		private static void CloseTmpImporterWindows()
		{
			foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
			{
				if (window.GetType().Name == "TMP_PackageResourceImporterWindow") window.Close();
			}
		}

		private static void OnImportFailed(string packageName, string errorMessage)
		{
			if (packageName != ESSENTIALS_PACKAGE_NAME) return;

			StopListening();
			Debug.LogWarning("All In 1: TMP Essentials could not be imported for the demo (" + errorMessage + "). Import them from Window > TextMeshPro > Import TMP Essential Resources.");
		}

		private static void StopListening()
		{
			pendingScenePath = null;
			AssetDatabase.importPackageCompleted -= OnImportCompleted;
			AssetDatabase.importPackageFailed -= OnImportFailed;
		}
	}
}
