using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace AllIn1Assets.AllIn1SpriteShader
{
    [CustomEditor(typeof(AllIn1Shader)), CanEditMultipleObjects]
    public class AllIn1ShaderScriptEditor : Editor
    {
        private bool showUrpWarning = false;
        private double warningTime = 0f;
        private SerializedProperty m_NormalStrength, m_NormalSmoothing;
        private Texture2D imageInspector;
    
        private enum ImageType
        {
            ShowImage,
            HideInComponent,
            HideEverywhere
        }
        private ImageType imageType;

        private void OnEnable()
        {
            m_NormalStrength = serializedObject.FindProperty("normalStrength");
            m_NormalSmoothing = serializedObject.FindProperty("normalSmoothing");

			EditorApplication.update += OnEditorUpdate;
		}

		private void OnDisable()
		{
			EditorApplication.update -= OnEditorUpdate;
		}


		protected void OnEditorUpdate()
		{
			for(int i = 0; i < targets.Length; i++)
			{
				AllIn1Shader assetComponent = (AllIn1Shader)targets[i];
				UpdateChecks(assetComponent);
				OnEditorUpdate(assetComponent);
			}
		}

		private void UpdateChecks(AllIn1Shader assetComponent)
		{
			if (assetComponent.matAssigned || Application.isPlaying || !assetComponent.gameObject.activeSelf) return;
			Renderer sr = assetComponent.GetComponent<Renderer>();
			if (sr != null)
			{
				if (sr.sharedMaterial == null)
				{
					assetComponent.CleanMaterial();
					MakeNewMaterial(assetComponent, true);
				}
				if (ShouldAutoReplaceMaterial(sr.sharedMaterial)) MakeNewMaterial(assetComponent, true);
				else assetComponent.matAssigned = true;
			}
			else
			{
				Graphic img = assetComponent.GetComponent<Graphic>();
				if (img != null)
				{
					if (ShouldAutoReplaceMaterial(img.material)) MakeNewMaterial(assetComponent, true);
					else assetComponent.matAssigned = true;
				}
			}
		}

		//Built in and package materials (URP's Lit on a MeshRenderer, for example) sit outside Assets and may lack "Default" in their names
		private static bool ShouldAutoReplaceMaterial(Material mat)
		{
			bool res = mat.name.Contains("Default") || EditorUtils.IsAssetOutsideAssetsFolder(mat);
			return res;
		}

		protected void OnEditorUpdate(AllIn1Shader assetComponent)
		{
			if (assetComponent.computingNormal)
			{
				if (assetComponent.needToWait)
				{
					assetComponent.waitingCycles++;
					if (assetComponent.waitingCycles > 5)
					{
						assetComponent.needToWait = false;
						assetComponent.timesWeWaited++;
					}
				}
				else
				{
					if (assetComponent.timesWeWaited == 1) SetNewNormalTexture2(assetComponent);
					if (assetComponent.timesWeWaited == 2) SetNewNormalTexture3(assetComponent);
					if (assetComponent.timesWeWaited == 3) SetNewNormalTexture4(assetComponent);
					assetComponent.needToWait = true;
				}
			}
		}

		public override void OnInspectorGUI()
        {
            ChooseAndDisplayAssetImage();

            AllIn1Shader myScript = (AllIn1Shader)target;

            SetCurrentShaderType(myScript);

            if (GUILayout.Button("Deactivate All Effects"))
            {
				for (int i = 0; i < targets.Length; i++) 
				{
					AllIn1Shader assetComponent = (AllIn1Shader)targets[i];
					assetComponent.ClearAllKeywords();

					//((AllIn1Shader)targets[i]).ClearAllKeywords(); 
				}

				SetSceneDirty();
                AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Deactivated All Effects");
            }

            if (GUILayout.Button("New Clean Material"))
            {
                bool successOperation = true;
                for (int i = 0; i < targets.Length; i++)
                {
					AllIn1Shader allIn1ShaderComponent = (AllIn1Shader)targets[i];
					bool tryCreateNewResult = TryCreateNew(allIn1ShaderComponent);

					successOperation &= tryCreateNewResult;
					//successOperation &= ((AllIn1Shader)targets[i]).TryCreateNew();
				}
                if(successOperation) AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Clean Material");
            }


            if (GUILayout.Button("Create New Material With Same Properties (SEE DOC)"))
            {
                bool successOperation = true;
                for (int i = 0; i < targets.Length; i++)
                {
					AllIn1Shader allIn1ShaderComponent = (AllIn1Shader)targets[i];
					bool makeCopyResult = MakeCopy(allIn1ShaderComponent);

					successOperation &= makeCopyResult;
					//successOperation &= ((AllIn1Shader)targets[i]).MakeCopy();
				}
                if(successOperation) AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Copy Created");
            }

            if (GUILayout.Button("Save Material To Folder (SEE DOC)"))
            {
                bool successOperation = true;
                for(int i = 0; i < targets.Length; i++)
                {
					AllIn1Shader assetComponent = (AllIn1Shader)targets[i];
					bool saveMaterialResult = SaveMaterial(assetComponent);
					successOperation &= saveMaterialResult;

					//successOperation &= ((AllIn1Shader) targets[i]).SaveMaterial();
				}
                if(successOperation) AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Material Saved");
            }

            if (GUILayout.Button("Apply Material To All Children"))
            {
                bool successOperation = true;
                for(int i = 0; i < targets.Length; i++)
                {
                    successOperation &= ((AllIn1Shader) targets[i]).ApplyMaterialToHierarchy();
                }
                if(successOperation) AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Material Applied To Children");
                else EditorUtility.DisplayDialog("No children found", "All In 1 Shader component couldn't find any children to this GameObject (" + targets[0].name + ")", "Ok");
            }

            if (myScript.currentShaderType != AllIn1Shader.ShaderTypes.Urp2dRenderer)
            {
                if (GUILayout.Button("Render Material To Image"))
                {
                    bool successOperation = true;
                    for(int i = 0; i < targets.Length; i++)
                    {
						AllIn1Shader allIn1ShaderComponent = (AllIn1Shader)targets[i];
						bool renderToImageResult = RenderToImage(allIn1ShaderComponent);

						successOperation &= renderToImageResult;
						//successOperation &= ((AllIn1Shader)targets[i]).RenderToImage();
					}
                    if(successOperation) AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Material Rendered To Image");
                }
            }

            bool isUrp = false;
            Shader temp = AllIn1ShaderWindow.FindShader("AllIn1Urp2dRenderer");
            if (temp != null) isUrp = true;
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Change Shader Variant:", GUILayout.MaxWidth(140));
                int previousShaderType = (int)myScript.currentShaderType;
                myScript.currentShaderType = (AllIn1Shader.ShaderTypes)EditorGUILayout.EnumPopup(myScript.currentShaderType);
                if (previousShaderType != (int)myScript.currentShaderType)
                {
                    for (int i = 0; i < targets.Length; i++) ((AllIn1Shader)targets[i]).CheckIfValidTarget();
                    if (myScript == null) return;
                    if (isUrp || myScript.currentShaderType != AllIn1Shader.ShaderTypes.Urp2dRenderer)
                    {
                        AllIn1ShaderWindow.SceneViewNotificationAndLog(myScript.gameObject.name + " shader variant has been changed to: " + myScript.currentShaderType);
                        SetSceneDirty();

                        Renderer sr = myScript.GetComponent<Renderer>();
                        if (sr != null)
                        {
                            if (sr.sharedMaterial != null)
                            {
                                int renderingQueue = sr.sharedMaterial.renderQueue;
                                if (myScript.currentShaderType == AllIn1Shader.ShaderTypes.Invalid) SetCurrentShaderType(myScript);
                                else sr.sharedMaterial.shader = AllIn1ShaderWindow.FindShader(AllIn1Shader.GetShaderName(myScript.currentShaderType));
                                sr.sharedMaterial.renderQueue = renderingQueue;

								if (myScript.currentShaderType == AllIn1Shader.ShaderTypes.Lit)
								{
									sr.sharedMaterial.SetFloat("_ZWrite", 1.0f);
									sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
									sr.receiveShadows = true;
                                    sr.sharedMaterial.renderQueue = 2000;
                                }
								else
								{
									sr.sharedMaterial.SetFloat("_ZWrite", 0f);
									sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                    sr.receiveShadows = false;
                                    sr.sharedMaterial.renderQueue = 3000;
								}
							}
                        }
                        else
                        {
							Graphic img = myScript.GetComponent<Graphic>();
                            if (img != null && img.material != null)
                            {
                                int renderingQueue = img.material.renderQueue;
                                if (myScript.currentShaderType == AllIn1Shader.ShaderTypes.Invalid) SetCurrentShaderType(myScript);
                                else img.material.shader = AllIn1ShaderWindow.FindShader(AllIn1Shader.GetShaderName(myScript.currentShaderType));
                                img.material.renderQueue = renderingQueue;
                            }
                        }
                    }
                    else if(!isUrp && myScript.currentShaderType == AllIn1Shader.ShaderTypes.Urp2dRenderer)
                    {
                        myScript.currentShaderType = (AllIn1Shader.ShaderTypes) previousShaderType;
                        showUrpWarning = true;
                        warningTime = EditorApplication.timeSinceStartup + 5;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (warningTime < EditorApplication.timeSinceStartup) showUrpWarning = false;
            if (isUrp) showUrpWarning = false;
            if (showUrpWarning) EditorGUILayout.HelpBox(
                "You can't set the URP 2D Renderer variant since you didn't import the URP package available in the asset root folder (SEE DOCUMENTATION)",
                MessageType.Error,
                true);

            if ((isUrp && myScript.currentShaderType == AllIn1Shader.ShaderTypes.Urp2dRenderer) || myScript.currentShaderType == AllIn1Shader.ShaderTypes.Lit
                || myScript.currentShaderType == AllIn1Shader.ShaderTypes.LitTransparent)
            {
                EditorGUILayout.Space();
                DrawLine(Color.grey, 1, 3);
                EditorGUILayout.Space();

                if (GUILayout.Button("Create And Add Normal Map"))
                {
					for (int i = 0; i < targets.Length; i++) 
					{
						AllIn1Shader assetComponent = (AllIn1Shader)targets[i];
						CreateAndAssignNormalMapEditor(assetComponent);

						//((AllIn1Shader)targets[i]).CreateAndAssignNormalMap(); 
					}
					
					AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Creating Normal Map"); 
                }
                serializedObject.Update();
                EditorGUILayout.PropertyField(m_NormalStrength, new GUIContent("Normal Strength"), GUILayout.Height(20));
                EditorGUILayout.PropertyField(m_NormalSmoothing, new GUIContent("Normal Blur"), GUILayout.Height(20));
                if (myScript.computingNormal)
                {
                    EditorGUILayout.LabelField("Normal Map is currently being created, be patient", EditorStyles.boldLabel, GUILayout.Height(40));
                }
                serializedObject.ApplyModifiedProperties();

                EditorGUILayout.Space();
            }

            DrawLine(Color.grey, 1, 3);
            EditorGUILayout.Space();

            if (GUILayout.Button("Sprite Atlas Auto Setup"))
            {
                bool successOperation = true;
                for(int i = 0; i < targets.Length; i++)
                {
                    successOperation &= ((AllIn1Shader) targets[i]).ToggleSetAtlasUvs(true);
                }
                if(successOperation) AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Sprite Atlas Auto Setup");
            }
            if (GUILayout.Button("Remove Sprite Atlas Configuration"))
            {
                bool successOperation = true;
                for(int i = 0; i < targets.Length; i++)
                {
                    successOperation &= ((AllIn1Shader) targets[i]).ToggleSetAtlasUvs(false);
                }
                if(successOperation) AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Remove Sprite Atlas Configuration");
            }
            
#if LETAI_TRUESHADOW
            if (myScript.GetComponent<LeTai.TrueShadow.TrueShadow>() && !myScript.GetComponent<TrueShadowCompatibility>())
            {
                EditorGUILayout.Space();
                DrawLine(Color.grey, 1, 3);
                if (GUILayout.Button("Add True Shadow Compatibility"))
                {
                    myScript.gameObject.AddComponent<TrueShadowCompatibility>();
                    SetSceneDirty();
                }
            }
#endif

            EditorGUILayout.Space();
            DrawLine(Color.grey, 1, 3);
            
            if(GUILayout.Button("Remove Component"))
            {
                for(int i = targets.Length - 1; i >= 0; i--)
                {
                    DestroyImmediate(targets[i] as AllIn1Shader);

					SetSceneDirty();
					//((AllIn1Shader)targets[i]).SetSceneDirty();
                }
                AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Component Removed");
            }

            if (GUILayout.Button("REMOVE COMPONENT AND MATERIAL"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
					AllIn1Shader assetComponent = (AllIn1Shader)targets[i];
					CleanMaterialEditor(assetComponent);

					//((AllIn1Shader)targets[i]).CleanMaterial();
                }
                for (int i = targets.Length - 1; i >= 0; i--)
                {
                    DestroyImmediate(targets[i] as AllIn1Shader);
                }
                AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Component And Material Removed");
            }
        }

		private void ChooseAndDisplayAssetImage()
        {

			AllIn1SpriteShaderConfig pluginConfig = PluginRegistry.GetPluginByID<AllIn1SpriteShaderConfig>(PluginID.ALLIN1_SPRITE);

			ImageType imageType = (ImageType)EditorPrefs.GetInt("allIn1ImageConfig", 0);
			if (imageType != ImageType.ShowImage) return;

			//GetInspectorImage, not the field: the field is only filled once the config has initialized
			Texture inspectorImage = pluginConfig == null ? null : pluginConfig.GetInspectorImage();
			if (inspectorImage == null) return;

			Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(40));
			GUI.DrawTexture(rect, inspectorImage, ScaleMode.ScaleToFit, true);

			/*
            if(!EditorPrefs.HasKey("allIn1ImageConfig"))
            {
                EditorPrefs.SetInt("allIn1ImageConfig", (int) ImageType.ShowImage);
            }

            imageType = (ImageType) EditorPrefs.GetInt("allIn1ImageConfig");
            switch(imageType)
            {
                case ImageType.ShowImage:
                case ImageType.HideInComponent:
                    if(imageInspector == null) imageInspector = AllIn1ShaderWindow.GetInspectorImage();
                    break;
            }

            if(imageInspector && imageType != ImageType.HideInComponent && imageType != ImageType.HideEverywhere && imageInspector)
            {
                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(40));
                GUI.DrawTexture(rect, imageInspector, ScaleMode.ScaleToFit, true);
            }*/
		}

        private void SetCurrentShaderType(AllIn1Shader myScript)
        {
            string shaderName = "";
            Renderer sr = myScript.GetComponent<Renderer>();
            if (sr != null)
            {
                if(sr.sharedMaterial == null) return;
                shaderName = sr.sharedMaterial.shader.name;
            }
            else
            {
                Graphic img = myScript.GetComponent<Graphic>();
                if (img == null || img.material == null) return;
                shaderName = img.material.shader.name;
            }
            shaderName = shaderName.Replace("AllIn1SpriteShader/", "");

            //Reverse of GetShaderName, so a new variant only needs adding there
            foreach (AllIn1Shader.ShaderTypes shaderType in System.Enum.GetValues(typeof(AllIn1Shader.ShaderTypes)))
            {
                if (shaderType != AllIn1Shader.ShaderTypes.Invalid && shaderName.Equals(AllIn1Shader.GetShaderName(shaderType)))
                {
                    myScript.currentShaderType = shaderType;
                    return;
                }
            }
		}

        private void DrawLine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += (padding / 2);
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

		public bool TryCreateNew(AllIn1Shader assetComponent)
		{
			bool rendererExists = false;
			Renderer sr = assetComponent.GetComponent<Renderer>();
			if (sr != null)
			{
				rendererExists = true;
				if (sr != null && sr.sharedMaterial != null && sr.sharedMaterial.name.Contains("AllIn1"))
				{
					ResetAllProperties(assetComponent, false, GetStringFromShaderType(assetComponent));
					ClearAllKeywords(assetComponent);
				}
				else
				{
					assetComponent.CleanMaterial();
					MakeNewMaterial(assetComponent, false, GetStringFromShaderType(assetComponent));
				}
			}
			else
			{
				Graphic img = assetComponent.GetComponent<Graphic>();
				if (img != null)
				{
					rendererExists = true;
					if (img.material.name.Contains("AllIn1"))
					{
						ResetAllProperties(assetComponent, false, GetStringFromShaderType(assetComponent));
						ClearAllKeywords(assetComponent);
					}
					else MakeNewMaterial(assetComponent, false, GetStringFromShaderType(assetComponent));
				}
			}
			if (!rendererExists)
			{
				assetComponent.MissingRenderer();
			}
			SetSceneDirty();
			return rendererExists;
		}

		private void CleanMaterialEditor(AllIn1Shader assetComponent)
		{
			assetComponent.CleanMaterial();
			SetSceneDirty();
		}

		private void ClearAllKeywords(AllIn1Shader assetComponent)
		{
			assetComponent.ClearAllKeywords();
			SetSceneDirty();
		}

		public bool SaveMaterial(AllIn1Shader assetComponent)
		{
			AllIn1SpriteShaderConfig pluginConfig = PluginRegistry.GetPluginByID<AllIn1SpriteShaderConfig>(PluginID.ALLIN1_SPRITE);

			string sameMaterialPath = /*AllIn1ShaderWindow.GetMaterialSavePath()*/pluginConfig.MaterialSavePath;
			sameMaterialPath += "/";
			if (!System.IO.Directory.Exists(sameMaterialPath))
			{
				EditorUtility.DisplayDialog("The desired Material Save Path doesn't exist",
					"Go to Tools -> AllIn1 -> Sprite Shader Window and set a valid folder", "Ok");
				return false;
			}
			sameMaterialPath += assetComponent.gameObject.name;
			string fullPath = sameMaterialPath + ".mat";
			if (System.IO.File.Exists(fullPath))
			{
				assetComponent.SaveMaterialWithOtherName(sameMaterialPath);
			}
			else assetComponent.DoSaving(fullPath);
			SetSceneDirty();
			return true;
		}

		public void SetSceneDirty()
		{
			if (!Application.isPlaying) EditorSceneManager.MarkAllScenesDirty();

			//If you get an error here please delete the code block below
			var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

			if (prefabStage != null) EditorSceneManager.MarkSceneDirty(prefabStage.scene);
			//Until here
		}

		public bool MakeCopy(AllIn1Shader assetComponent)
		{
			string shaderName = GetStringFromShaderType(assetComponent);
			bool res = SetMaterial(assetComponent, AllIn1Shader.AfterSetAction.CopyMaterial, false, shaderName);
			return res;
		}

		public bool RenderToImage(AllIn1Shader assetComponent)
		{
			if (assetComponent.currMaterial == null)
			{
				assetComponent.FindCurrMaterial();
				if (assetComponent.currMaterial == null)
				{
					assetComponent.MissingRenderer();
					return false;
				}
			}
			Texture tex = assetComponent.currMaterial.GetTexture("_MainTex");
			if (tex != null)
			{
				bool success = RenderAndSaveTexture(assetComponent, assetComponent.currMaterial, tex);
				if (!success) return false;
			}
			else
			{
				SpriteRenderer sr = assetComponent.GetComponent<SpriteRenderer>();
				Graphic i = assetComponent.GetComponent<Graphic>();
				if (sr != null && sr.sprite != null && sr.sprite.texture != null) tex = sr.sprite.texture;
				else if (i != null && i.mainTexture != null) tex = i.mainTexture;

				if (tex != null)
				{
					bool success = RenderAndSaveTexture(assetComponent, assetComponent.currMaterial, tex);
					if (!success) return false;
				}
				else
				{
					EditorUtility.DisplayDialog("No valid target texture found", "All In 1 Shader component couldn't find a valid Main Texture in this GameObject (" +
																				  assetComponent.gameObject.name + "). This means that the material you are using has no Main Texture or that the texture couldn't be reached through the Renderer component you are using." +
																				  " Please make sure to have a valid Main Texture in the Material or Renderer/Graphic component", "Ok");
					return false;
				}
			}
			return true;
		}

		public bool RenderAndSaveTexture(AllIn1Shader allIn1ShaderComponent, Material targetMaterial, Texture targetTexture)
		{
			AllIn1SpriteShaderConfig pluginConfig = (AllIn1SpriteShaderConfig)PluginRegistry.GetPluginByID<AllIn1SpriteShaderConfig>(PluginID.ALLIN1_SPRITE);

			float scaleSlider = 1;
			if (PlayerPrefs.HasKey(AllIn1Shader.PREF_RENDER_IMAGES_SCALE)) scaleSlider = PlayerPrefs.GetFloat(AllIn1Shader.PREF_RENDER_IMAGES_SCALE);
			RenderTexture renderTarget = new RenderTexture((int)(targetTexture.width * scaleSlider), (int)(targetTexture.height * scaleSlider), 0, RenderTextureFormat.ARGB32);
			Graphics.Blit(targetTexture, renderTarget, targetMaterial);
			Texture2D reaultTex = new Texture2D(renderTarget.width, renderTarget.height, TextureFormat.ARGB32, false);
			reaultTex.ReadPixels(new Rect(0, 0, renderTarget.width, renderTarget.height), 0, 0);
			reaultTex.Apply();

			/*string path = AllIn1ShaderWindow.GetRenderImageSavePath();*/
			string path = pluginConfig.RenderImageSavePath;

			path += "/";
			if (!System.IO.Directory.Exists(path))
			{
				EditorUtility.DisplayDialog("The desired Material to Image Save Path doesn't exist",
					"Go to Tools -> AllIn1 -> Sprite Shader Window and set a valid folder", "Ok");
				return false;
			}
			string fullPath = path + allIn1ShaderComponent.gameObject.name + ".png";
			if (System.IO.File.Exists(fullPath)) fullPath = GetNewValidPath(path + allIn1ShaderComponent.gameObject.name);
			string pingPath = fullPath;

			string fileName = fullPath.Replace(path, "");
			fileName = fileName.Replace(".png", "");
			fullPath = EditorUtility.SaveFilePanel("Save Render Image", path, fileName, "png");
			if (string.IsNullOrEmpty(fullPath))
			{
				Debug.Log("Save operation was cancelled or no valid path was selected.");
				return false;
			}

			byte[] bytes = reaultTex.EncodeToPNG();
			File.WriteAllBytes(fullPath, bytes);
			AssetDatabase.ImportAsset(allIn1ShaderComponent.subPath);
			AssetDatabase.Refresh();
			DestroyImmediate(reaultTex);
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(pingPath, typeof(Texture)));
			Debug.Log("Render Image saved to: " + fullPath + " with scale: " + scaleSlider + " (it can be changed in Tools -> AllIn1 -> Sprite Shader Window)");

			return true;
		}

		private string GetNewValidPath(string path, int i = 1)
		{
			int number = i;
			string newPath = path + "_" + number.ToString();
			string fullPath = newPath + ".png";
			if (System.IO.File.Exists(fullPath))
			{
				number++;
				fullPath = GetNewValidPath(path, number);
			}
			return fullPath;
		}

		private void SetNewNormalTexture2(AllIn1Shader assetComponent)
		{
			if (!assetComponent.isSpriteRenderer)
			{
				assetComponent.mainTex2D = (Texture2D)assetComponent.normalMapRenderer.sharedMaterial.GetTexture("_MainTex");
				assetComponent.importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(assetComponent.mainTex2D)) as TextureImporter;
			}
			else assetComponent.importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(assetComponent.normalMapSr.sprite)) as TextureImporter;

			assetComponent.importer.isReadable = true;
			assetComponent.importer.SaveAndReimport();
		}

		private void SetNewNormalTexture3(AllIn1Shader assetComponent)
		{
			Texture2D normalM = null;
			if (assetComponent.isSpriteRenderer) normalM = AllIn1ShaderWindow.CreateNormalMap(assetComponent.normalMapSr.sprite.texture, assetComponent.normalStrength, assetComponent.normalSmoothing);
			else normalM = AllIn1ShaderWindow.CreateNormalMap(assetComponent.mainTex2D, assetComponent.normalStrength, assetComponent.normalSmoothing);

			byte[] bytes = normalM.EncodeToPNG();

			assetComponent.path += assetComponent.gameObject.name;
			assetComponent.subPath = assetComponent.path + ".png";
			string dataPath = Application.dataPath;
			dataPath = dataPath.Replace("/Assets", "/");
			string fullPath = dataPath + assetComponent.subPath;

			File.WriteAllBytes(fullPath, bytes);
			AssetDatabase.ImportAsset(assetComponent.subPath);
			AssetDatabase.Refresh();
			DestroyImmediate(normalM);
		}

		private void SetNewNormalTexture4(AllIn1Shader assetComponent)
		{
			assetComponent.importer = AssetImporter.GetAtPath(assetComponent.subPath) as TextureImporter;
			assetComponent.importer.filterMode = FilterMode.Bilinear;
			assetComponent.importer.textureType = TextureImporterType.NormalMap;
			assetComponent.importer.wrapMode = TextureWrapMode.Repeat;
			assetComponent.importer.SaveAndReimport();

			if (assetComponent.currMaterial == null)
			{
				assetComponent.FindCurrMaterial();
				if (assetComponent.currMaterial == null)
				{
					assetComponent.MissingRenderer();
					return;
				}
			}
			Texture2D normalTex = (Texture2D)AssetDatabase.LoadAssetAtPath(assetComponent.subPath, typeof(Texture2D));
			assetComponent.currMaterial.SetTexture("_NormalMap", normalTex);

			Debug.Log("Normal texture saved to: " + assetComponent.subPath);
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(assetComponent.subPath, typeof(Texture)));

			assetComponent.computingNormal = false;
		}

		private bool SetMaterial(AllIn1Shader assetComponent, AllIn1Shader.AfterSetAction action, bool getShaderTypeFromPrefs, string shaderName)
		{
#if UNITY_EDITOR
			if (getShaderTypeFromPrefs)
			{
				assetComponent.currentShaderType = AllIn1SpriteShaderConfig.DefaultShaderType;
				shaderName = AllIn1Shader.GetShaderName(assetComponent.currentShaderType);
			}

			Shader allIn1Shader = AllIn1ShaderWindow.FindShader(shaderName);

			if (!Application.isPlaying && Application.isEditor && allIn1Shader != null)
			{
				bool rendererExists = false;
				Renderer sr = assetComponent.GetComponent<Renderer>();
				if (sr != null)
				{
					rendererExists = true;
					int renderingQueue = 3000;
					if (action == AllIn1Shader.AfterSetAction.CopyMaterial) renderingQueue = assetComponent.GetComponent<Renderer>().sharedMaterial.renderQueue;
					assetComponent.prevMaterial = new Material(assetComponent.GetComponent<Renderer>().sharedMaterial);
					assetComponent.currMaterial = new Material(allIn1Shader);
					assetComponent.currMaterial.renderQueue = renderingQueue;
					assetComponent.GetComponent<Renderer>().sharedMaterial = assetComponent.currMaterial;
					assetComponent.GetComponent<Renderer>().sharedMaterial.hideFlags = HideFlags.None;
					assetComponent.matAssigned = true;

					DoAfterSetAction(assetComponent, action);
				}
				else
				{
					Graphic img = assetComponent.GetComponent<Graphic>();
					if (img != null)
					{
						rendererExists = true;
						int renderingQueue = 3000;
						if (action == AllIn1Shader.AfterSetAction.CopyMaterial) renderingQueue = img.material.renderQueue;
						assetComponent.prevMaterial = new Material(img.material);
						assetComponent.currMaterial = new Material(allIn1Shader);
						assetComponent.currMaterial.renderQueue = renderingQueue;
						img.material = assetComponent.currMaterial;
						img.material.hideFlags = HideFlags.None;
						assetComponent.matAssigned = true;
						DoAfterSetAction(assetComponent, action);
					}
				}
				if (!rendererExists)
				{
					assetComponent.MissingRenderer();
					return false;
				}
				else
				{
					SetSceneDirty();
					return true;
				}
			}
			else if (allIn1Shader == null)
			{
	#if UNITY_EDITOR
					string logErrorMessage = "Make sure all AllIn1SpriteShader shader variants are present. Maybe delete the asset and download it again?";
					Debug.LogError(logErrorMessage);
					AllIn1ShaderWindow.ShowSceneViewNotification(logErrorMessage);
	#endif
				return false;
			}
#endif
			return false;
		}

		private void DoAfterSetAction(AllIn1Shader assetComponent, AllIn1Shader.AfterSetAction action)
		{
			switch (action)
			{
				case AllIn1Shader.AfterSetAction.Clear:
					assetComponent.ClearAllKeywords();
					SetSceneDirty();
					break;
				case AllIn1Shader.AfterSetAction.CopyMaterial:
					assetComponent.currMaterial.CopyPropertiesFromMaterial(assetComponent.prevMaterial);
					break;
			}
		}

		private void ResetAllProperties(AllIn1Shader assetComponent, bool getShaderTypeFromPrefs, string shaderName)
		{
			SetMaterial(assetComponent, AllIn1Shader.AfterSetAction.Reset, getShaderTypeFromPrefs, shaderName);
		}

		//Forcing currentShaderType to Default here made every branch below it dead code, so every new
		//material came out as the Default variant and the component's own variant was overwritten
		private string GetStringFromShaderType(AllIn1Shader assetComponent)
		{
			return AllIn1SpriteShaderConfig.ResolveShaderName(assetComponent.currentShaderType);
		}

		private void MakeNewMaterial(AllIn1Shader assetComponent, bool getShaderTypeFromPrefs, string shaderName = "AllIn1SpriteShader")
		{
			bool operationSuccessful = SetMaterial(assetComponent, AllIn1Shader.AfterSetAction.Clear, getShaderTypeFromPrefs, shaderName);
#if UNITY_EDITOR
			if (operationSuccessful) AllIn1ShaderWindow.ShowSceneViewNotification("AllIn1SpriteShader: Material Created and Assigned");
#endif
		}

		public bool ToggleSetAtlasUvsEditor(AllIn1Shader assetComponent, bool activate)
		{
			bool success = assetComponent.ToggleSetAtlasUvs(activate);

			SetSceneDirty();

			return success;
		}

		public void CreateAndAssignNormalMapEditor(AllIn1Shader assetComponent)
		{
			AllIn1SpriteShaderConfig allIn1SpriteShaderConfig = PluginRegistry.GetPluginByID<AllIn1SpriteShaderConfig>(PluginID.ALLIN1_SPRITE);
			string path = allIn1SpriteShaderConfig.NormalMapSavePath;
			assetComponent.CreateAndAssignNormalMap(path);
		}
	}
}