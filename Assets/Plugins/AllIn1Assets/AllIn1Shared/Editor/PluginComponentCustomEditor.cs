using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AllIn1Assets
{
	public abstract class PluginComponentCustomEditor : Editor
	{
		private const string TAG = "PluginComponentCustomEditor";

		protected AbstractPluginConfig pluginConfigGeneric;

		[NonSerialized] private bool configFailureReported;

		protected abstract PluginID GetPluginID();
		protected abstract void DisplayNotValidComponentsDialog();

		//The first access resolves and initializes the config, which can fail while assets are still
		//importing or right after a reset. Reported once, then retried from the next repaint
		protected void RefreshReferences()
		{
			try
			{
				PluginID pluginID = GetPluginID();
				pluginConfigGeneric = PluginRegistry.GetPluginByID<AbstractPluginConfig>(pluginID);

				if (pluginConfigGeneric != null)
				{
					configFailureReported = false;
				}
			}
			catch (Exception e)
			{
				pluginConfigGeneric = null;

				if (!configFailureReported)
				{
					configFailureReported = true;
					Debug.LogWarning($"[AllIn1] {GetType().Name} could not load its configuration yet, it will retry. Details: {e}");
				}
			}
		}

		protected virtual void OnEnableImpl()
		{
			RefreshReferences();

			bool isPartOfAnyPrefab = PrefabUtility.IsPartOfAnyPrefab(target);

			if(isPartOfAnyPrefab) { return; }
			if (pluginConfigGeneric == null) { return; }

			bool isValidComponents = CheckSelectedComponents();
			if (!isValidComponents)
			{
				DisplayNotValidComponentsDialog();
				return;
			}

			CheckMaterialReference();
		}

		protected virtual void OnDisableImpl()
		{
			
		}

		protected virtual void PlayModeStateChanged(PlayModeStateChange playModeStateChange)
		{
			
		}

		private void Awake()
		{

		}

		protected void OnEnable()
		{
			EditorApplication.playModeStateChanged += PlayModeStateChanged;
			
			if (!Application.isPlaying)
			{
				OnEnableImpl();
			}
		}

		protected void OnDisable()
		{
			EditorApplication.playModeStateChanged -= PlayModeStateChanged;

			OnDisableImpl();
		}

		public override void OnInspectorGUI()
		{
			if (Application.isPlaying) { return; }

			//A reset or a still running import can leave this null after OnEnable already gave up
			if (pluginConfigGeneric == null)
			{
				RefreshReferences();
			}

			if (pluginConfigGeneric == null)
			{
				EditorGUILayout.HelpBox("The plugin configuration is not ready yet. It is created once the assets finish importing, " +
					"so this clears on its own after the import or the reset that is running finishes.", MessageType.Info);
				return;
			}

			bool saveAssets = false;

			serializedObject.Update();

			DrawHeaderImage();

			EditorGUI.BeginDisabledGroup(Application.isPlaying);

			if (GUILayout.Button("Deactivate All Effects"))
			{
				ExecuteActionAfterCheck(DeactivateAllEffectsOfSelection);
				saveAssets = true;
			}

			if (GUILayout.Button("New Clean Material"))
			{
				NewCleanMaterial();
			}

			if (GUILayout.Button("Save Material To Folder (SEE DOC)"))
			{
				SaveMaterialToFolder();
			}

			DrawExtraButtons();

			EditorGUI.EndDisabledGroup();

			if (saveAssets)
			{
				AssetDatabase.SaveAssets();
				EditorSceneManager.SaveOpenScenes();
			}

			DrawExtraSections();

			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.Space();
			EditorUtils.DrawThinLine();

			if (GUILayout.Button("Remove Component"))
			{
				RemoveComponentButtonAction();
			}

			if (GUILayout.Button("Remove Component and Material"))
			{
				RemoveComponentAndMaterialButtonAction();
			}

			
		}

		private void DrawHeaderImage()
		{
			//Asked again every repaint rather than cached: the image can land after the inspector opened
			Texture inspectorImage = pluginConfigGeneric.GetInspectorImage();
			if (inspectorImage == null) return;

			Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(32));
			GUI.DrawTexture(rect, inspectorImage, ScaleMode.ScaleToFit, true);
		}

		private bool CheckSelectedComponents()
		{
			bool res = true;

			for (int i = 0; i < targets.Length; i++)
			{
				PluginComponent pluginComponent = (PluginComponent)targets[i];

				bool isValid = pluginComponent.CheckValidComponent();
				if (!isValid)
				{
					DestroyImmediate(pluginComponent);
				}

				res = res && isValid;
			}

			return res;
		}

		private void DeactivateAllEffectsOfSelection()
		{
			for (int i = 0; i < targets.Length; i++)
			{
				PluginComponent pluginComponent = (PluginComponent)targets[i];

				Material mat = pluginComponent.GetCurrentMaterial();
				if (mat == null) continue;

				Undo.RecordObject(mat, "Deactivate All Effects");
				DeactivateAllEffects(mat);
				EditorUtility.SetDirty(mat);
			}

			SetSceneDirty();
		}

		//Only the plugin knows its effect list, so the base leaves the material as it is
		protected virtual void DeactivateAllEffects(Material mat)
		{

		}

		private void NewCleanMaterial()
		{
			for (int i = 0; i < targets.Length; i++)
			{
				PluginComponent pluginComponent = (PluginComponent)targets[i];

				Material defaultMaterial = pluginConfigGeneric.GetDefaultMaterial();
				pluginComponent.NewCleanMaterial(defaultMaterial);
				SaveMaterialIfInPrefabMode(pluginComponent);
			}
		}

		//A prefab only keeps a saved material, so one made in Prefab Mode is saved with the others. Otherwise saving the prefab dropped it
		//and the object came back without its material
		protected void SaveMaterialIfInPrefabMode(PluginComponent comp)
		{
			PrefabStage stage = comp != null ? PrefabStageUtility.GetPrefabStage(comp.gameObject) : null;
			if (stage == null) { return; }

			Material mat = comp.GetCurrentMaterial();
			if (mat == null || AssetDatabase.Contains(mat)) { return; }

			string folderPath = pluginConfigGeneric.GetMaterialSavePath();
			if (!EditorUtils.EnsureAssetFolderExists(folderPath))
			{
				Debug.LogWarning($"[AllIn1] {comp.name}'s new material could not be saved to '{folderPath}', so the prefab will not keep it. " +
					"Set a valid folder in the Save Paths tab of the plugin window, then use Save Material To Folder.");
				return;
			}

			string fullPath = AssetDatabase.GenerateUniqueAssetPath(EditorUtils.CombineAssetPath(folderPath, $"MAT_{pluginConfigGeneric.GetPluginName()}.mat"));
			AssetDatabase.CreateAsset(mat, fullPath);
			EditorSceneManager.MarkSceneDirty(stage.scene);
		}

		private void SaveMaterialToFolder()
		{
			for (int i = 0; i < targets.Length; i++)
			{
				PluginComponent pluginComponent = (PluginComponent)targets[i];
				SaveMaterialToFolder(pluginComponent);
			}

			EditorUtils.SetDirtyCurrentScene();
		}
		private void SaveMaterialToFolder(PluginComponent comp)
		{
			Material matToSave = comp.GetCurrentMaterial();
			bool isAlreadySaved = AssetDatabase.Contains(matToSave);
			if (isAlreadySaved)
			{
				matToSave = comp.DuplicateCurrentMaterial();
			}

			string folderPath = pluginConfigGeneric.GetMaterialSavePath();
			if (!Directory.Exists(folderPath))
			{
				bool ok = EditorUtility.DisplayDialog("The desired save folder doesn't exist",
					"Go to Tools -> AllIn1, open this plugin's window and set a valid folder", "Set default values and save material", "Cancel");

				if (ok)
				{
					Directory.CreateDirectory(folderPath);
				}
			}

			if (Directory.Exists(folderPath))
			{

				string matName = $"MAT_{pluginConfigGeneric.GetPluginName()}.mat";
				string fullPath = EditorUtils.CombineAssetPath(folderPath, matName);
				fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

				AssetDatabase.CreateAsset(matToSave, fullPath);
				/*AssetDatabase.Refresh();*/
				if (isAlreadySaved)
				{
					comp.SetCurrentMaterial(matToSave);
				}

				EditorGUIUtility.PingObject(matToSave);
			}
		}

		private void CheckMaterialReference()
		{
			for (int i = 0; i < targets.Length; i++)
			{
				PluginComponent pluginComponent = (PluginComponent)targets[i];

				Material currMaterial = pluginComponent.GetCurrentMaterial();
				if (ShouldAutoReplaceMaterial(currMaterial))
				{
					Material defaultMaterial = pluginConfigGeneric.GetDefaultMaterial();
					pluginComponent.NewCleanMaterial(defaultMaterial);
					SaveMaterialIfInPrefabMode(pluginComponent);

					AllIn1Logger.LogWarning(TAG, "Creating new material");
				}
			}

			EditorUtils.SetDirtyCurrentScene();
		}

		//Decides whether the material found on the object when the component is added gets replaced by the plugin's default material.
		//Subclasses can restrict this so custom materials are never clobbered
		protected virtual bool ShouldAutoReplaceMaterial(Material currMaterial)
		{
			return currMaterial == null || !pluginConfigGeneric.IsMaterialFromThisPlugin(currMaterial);
		}

		public void SetSceneDirty()
		{
			if (!Application.isPlaying) EditorSceneManager.MarkAllScenesDirty();

			//If you get an error here please delete the code block below
			var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null)
			{
				EditorSceneManager.MarkSceneDirty(prefabStage.scene);
			}

			//Until here
		}

		private void RemoveComponent()
		{
			for (int i = targets.Length - 1; i >= 0; i--)
			{
				PluginComponent pluginComponent = (PluginComponent)targets[i];

				pluginComponent.RemoveChildComponents();
				DestroyImmediate(pluginComponent);
			}
		}

		private void RemoveMaterial()
		{
			for (int i = 0; i < targets.Length; i++)
			{
				PluginComponent pluginComponent = (PluginComponent)targets[i];
				Shader newShader = pluginConfigGeneric.GetShaderAfterMaterialRemoval();

				pluginComponent.CleanMaterial(newShader);
			}
		}

		private void RemoveComponentButtonAction()
		{
			RemoveComponent();
			SetSceneDirty();
			ShowNotification($"{pluginConfigGeneric.GetPluginName()}: Component Removed");
		}

		private void RemoveComponentAndMaterialButtonAction()
		{
			RemoveMaterial();
			RemoveComponent();
			SetSceneDirty();
			ShowNotification($"{pluginConfigGeneric.GetPluginName()}: Component and Material Removed");
		}

		private void ShowNotification(string message)
		{
			EditorUtils.ShowNotification(message);
		}

		protected void ExecuteActionAfterCheck(Action action)
		{
			bool selectedComponentsAreValid = CheckSelectedComponents();

			if (selectedComponentsAreValid)
			{
				action();
			}
			else if (SceneView.lastActiveSceneView != null)
			{
				SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Some of the selected components are not valid"));
			}
		}

		protected abstract void DrawExtraButtons();

		protected abstract void DrawExtraSections();

		protected void DrawSectionSeparator()
		{
			EditorGUILayout.Space();
			EditorUtils.DrawThinLine();
		}
	}
}