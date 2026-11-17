using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AllIn1Assets
{
	public static class EditorUtils
	{
		private static T FindAssetByFilter<T>(string filter) where T : Object
		{
			T res = null;

			string[] guids = AssetDatabase.FindAssets(filter);

			if (guids.Length > 0)
			{
				for (int i = 0; i < guids.Length; i++)
				{
					string path = AssetDatabase.GUIDToAssetPath(guids[i]);
					res = AssetDatabase.LoadAssetAtPath<T>(path);

					if (res != null)
					{
						break;
					}
				}
			}

			return res;
		}

		private static List<T> FindAllAssetsByFilter<T>(string filter) where T : Object
		{
			List<T> res = new List<T>();

			string[] guids = AssetDatabase.FindAssets(filter);

			if (guids.Length > 0)
			{
				for (int i = 0; i < guids.Length; i++)
				{
					string path = AssetDatabase.GUIDToAssetPath(guids[i]);
					T loadedAsset = AssetDatabase.LoadAssetAtPath<T>(path);

					if (loadedAsset != null)
					{
						res.Add(loadedAsset);
					}
				}
			}

			return res;
		}

		//AssetDatabase.FindAssets matches substrings, so 'AllIn1SharedAssembly' also returns
		//'AllIn1SharedAssembly.Editor'. Only an exact file name is the asset that was asked for
		public static T FindAssetByName<T>(string assetName) where T : Object
		{
			if (string.IsNullOrEmpty(assetName))
			{
				return null;
			}

			string filter = $"{assetName} t:{typeof(T).Name}";
			string[] guids = AssetDatabase.FindAssets(filter);

			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);

				if (Path.GetFileNameWithoutExtension(path) != assetName) continue;

				T loadedAsset = AssetDatabase.LoadAssetAtPath<T>(path);
				if (loadedAsset != null)
				{
					return loadedAsset;
				}
			}

			return null;
		}

		public static T FindAsset<T>() where T : Object
		{
			T res = null;

			string filter = $"t:{typeof(T).Name}";
			res = FindAssetByFilter<T>(filter);

			return res;
		}

		public static List<T> FindAllAssetsByType<T>() where T : Object
		{
			string filter = $"t:{typeof(T).Name}";
			List<T> res = FindAllAssetsByFilter<T>(filter);

			return res;
		}

		public static void PingPath(string assetPath)
		{
			Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture));
			if(asset != null)
			{
				EditorGUIUtility.PingObject(asset);
			}
		}

		public static void ShowNotification(string message, double fadeOutWait = 1.0)
		{
			// No Scene view open (or batch mode): the message is only a toast, and throwing here aborted the save that called it
			if (SceneView.lastActiveSceneView == null) { return; }
			SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message), fadeOutWait);
		}

		public static void DrawThinLine()
		{
			DrawLine(Color.grey, 1, 3);
		}

		public static void DrawLine(Color color, int thickness = 2, int padding = 10)
		{
			Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
			r.height = thickness;
			r.y += padding / 2;
			r.x -= 2;
			r.width += 4;
			EditorGUI.DrawRect(r, color);
		}

		public static void SetTextureReadWrite(string assetPath, bool enable)
		{
			TextureImporter tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
			if (tImporter != null)
			{
				tImporter.isReadable = enable;
				tImporter.SaveAndReimport();
			}
		}

		public static string DrawSelectorFolder(string initialPath, string label)
		{
			DefaultAsset folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(initialPath);
			folderAsset = (DefaultAsset)EditorGUILayout.ObjectField(label, folderAsset, typeof(DefaultAsset), false, GUILayout.MaxWidth(500));

			string pathCandidate = AssetDatabase.GetAssetPath(folderAsset);

			string res = initialPath;
			if (Directory.Exists(pathCandidate))
			{
				res = pathCandidate;
			}

			return res;
		}

		public static bool IsPathInsideTheProject(string path)
		{
			string[] directories = NormalizeAssetPath(path).Split('/');

			bool res = directories.Contains(GlobalConstants.FOLDER_NAME_ASSET);
			return res;
		}

		//An absolute path from a save panel, cut down to the asset path AssetDatabase understands
		public static string CutPathToStartFromAsset(string path)
		{
			string res = string.Empty;

			string[] directories = NormalizeAssetPath(path).Split('/');
			bool assetFolderFound = false;
			for (int i = 0; i < directories.Length; i++)
			{
				if (!assetFolderFound)
				{
					if (directories[i] == GlobalConstants.FOLDER_NAME_ASSET)
					{
						assetFolderFound = true;
						res = directories[i];
					}

					continue;
				}

				res = CombineAssetPath(res, directories[i]);
			}

			return res;
		}

		public static Texture SaveTextureAsPNG(string folderPath, string fileName, string prefixNotification, Texture2D texture,
			FilterMode filterMode, TextureImporterType importerType, TextureWrapMode wrapMode,
			bool askForLocation = true, bool generateUniqueAssetPath = true, bool pingAsset = true, string importerUserData = null)
		{
			Texture res = null;

			string fileNameWithExtension = fileName + ".png";
			string path = CombineAssetPath(folderPath, fileNameWithExtension);

			if (generateUniqueAssetPath)
			{
				path = AssetDatabase.GenerateUniqueAssetPath(path);
			}
			
			fileName = Path.GetFileNameWithoutExtension(path);
			
			if (askForLocation)
			{
				path = EditorUtility.SaveFilePanel("Save texture as PNG", folderPath, fileName, "png");
			}

			if (!string.IsNullOrEmpty(path))
			{
				byte[] pngData = texture.EncodeToPNG();
				if (pngData != null) File.WriteAllBytes(path, pngData);
				AssetDatabase.Refresh();

				//string[] directories= path.Split(Path.DirectorySeparatorChar);
				//bool isFolderInsideTheProject = directories[0] == "Assets";

				if (IsPathInsideTheProject(path))
				{
					string subPath = CutPathToStartFromAsset(path);
					TextureImporter importer = AssetImporter.GetAtPath(subPath) as TextureImporter;
					if (importer != null)
					{
						ShowNotification($"{prefixNotification} saved inside the project: " + subPath);
						
						importer.filterMode = filterMode;
						importer.textureType = importerType;
						importer.wrapMode = wrapMode;
						if (importerUserData != null) importer.userData = importerUserData;

						importer.SaveAndReimport();
						res = AssetDatabase.LoadAssetAtPath<Texture>(subPath);
						if (pingAsset) EditorGUIUtility.PingObject(res);
					}
				}
				else 
				{
					ShowNotification($"{prefixNotification} saved outside the project: " + path);
				}
			}

			return res;
		}

		public static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
		{
			targetWidth = Mathf.ClosestPowerOfTwo(targetWidth);
			targetHeight = Mathf.ClosestPowerOfTwo(targetHeight);

			Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
			Color[] scaledPixels = result.GetPixels(0);
			float incX = ((float)1 / source.width) * ((float)source.width / targetWidth);
			float incY = ((float)1 / source.height) * ((float)source.height / targetHeight);
			for (int px = 0; px < scaledPixels.Length; px++) scaledPixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth), incY * (float)Mathf.Floor(px / targetWidth));

			result.SetPixels(scaledPixels, 0);
			result.Apply();
			return result;
		}

		public static void TextureEditorFloatParameter(string parameterName, ref float parameter, float rangeMin = -100f, float rangeMax = 100f, float resetValue = 0f)
		{
			EditorGUILayout.BeginHorizontal();
			{
				parameter = EditorGUILayout.Slider(parameterName, parameter, rangeMin, rangeMax, GUILayout.MaxWidth(400));
				GUIContent resetButtonLabel = new GUIContent
				{
					text = "R",
					tooltip = "Resets to default value"
				};
				if (GUILayout.Button(resetButtonLabel, GUILayout.Width(20))) parameter = resetValue;
			}
			EditorGUILayout.EndHorizontal();
		}

		public static void TextureEditorColorParameter(string parameterName, ref Color parameter, Color resetValue)
		{
			EditorGUILayout.BeginHorizontal();
			{
				GUIContent colorLabel = new GUIContent
				{
					text = parameterName,
					tooltip = parameterName
				};
				parameter = EditorGUILayout.ColorField(colorLabel, parameter, true, true, true, GUILayout.MaxWidth(400));
				GUIContent resetButtonLabel = new GUIContent
				{
					text = "R",
					tooltip = "Resets to default value"
				};
				if (GUILayout.Button(resetButtonLabel, GUILayout.Width(20))) parameter = resetValue;
			}
			EditorGUILayout.EndHorizontal();
		}

		public static void TextureEditorIntParameter(string parameterName, ref int parameter, int rangeMin = -100, int rangeMax = 100, int resetValue = 0)
		{
			EditorGUILayout.BeginHorizontal();
			{
				parameter = EditorGUILayout.IntSlider(parameterName, parameter, rangeMin, rangeMax, GUILayout.MaxWidth(400));
				GUIContent resetButtonLabel = new GUIContent
				{
					text = "R",
					tooltip = "Resets to default value"
				};
				if (GUILayout.Button(resetButtonLabel, GUILayout.Width(20))) parameter = resetValue;
			}
			EditorGUILayout.EndHorizontal();
		}

		public static Shader FindShader(string shaderName)
		{
			string[] guids = AssetDatabase.FindAssets($"{shaderName} t:shader");
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
				if (shader != null)
				{
					string fullShaderName = shader.name;
					string actualShaderName = fullShaderName.Substring(fullShaderName.LastIndexOf('/') + 1);
					if (actualShaderName == shaderName) return shader;
				}
			}
			return null;
		}

		public static void SetDirtyCurrentScene()
		{
			if (!Application.isPlaying)
			{
				EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
			}
		}

		public static bool IsProjectAsset(Object objectToCheck)
		{
			bool res = false;

			if(objectToCheck != null)
			{
				res = AssetDatabase.Contains(objectToCheck);
			}
			return res;
		}

		//Built in and package assets, which users cannot have customised. An object with no asset behind it is not one
		public static bool IsAssetOutsideAssetsFolder(Object objectToCheck)
		{
			if (objectToCheck == null) return false;

			string assetPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(objectToCheck));
			if (string.IsNullOrEmpty(assetPath)) return false;

			bool res = !assetPath.StartsWith(GlobalConstants.FOLDER_NAME_ASSET + "/", System.StringComparison.Ordinal);
			return res;
		}

		public static int GetNumLines(string input)
		{
			int res = input.Split("\n").Length;
			return res;
		}

		public static Color GetRandomColor()
		{
			Color res = new Color(Random.value, Random.value, Random.value, 1.0f);
			return res;
		}

		public static void DrawButtonLink(string url)
		{
			if (EditorGUILayout.LinkButton(url))
			{
				Application.OpenURL(url);
			}
		}

		public static bool ConstainsFileName(string[] pathArray, string fileName)
		{
			bool res = false;

			for(int i = 0; i < pathArray.Length; i++)
			{
				if (pathArray[i].EndsWith(fileName))
				{
					res = true;
					break;
				}
			}

			return res;
		}

		public static bool ContainsAnyFolowingFileNames(string[] pathArray, string[] fileNames, string extension)
		{
			bool res = false;

			for(int i = 0; i < pathArray.Length; i++)
			{
				for(int j = 0; j < fileNames.Length; j++)
				{
					string fileName = $"{fileNames[j]}.{extension}";

					if (pathArray[i].EndsWith(fileName))
					{
						res = true;
						break;
					}
				}

				if (res)
				{
					break;
				}
			}

			return res;
		}

		public static AllIn1ShaderPropertyType GetShaderTypeByMaterialProperty(MaterialProperty matProperty)
		{
			AllIn1ShaderPropertyType res;

#if UNITY_6000_2_OR_NEWER
			switch (matProperty.propertyType)
			{
				case UnityEngine.Rendering.ShaderPropertyType.Color:
					res = AllIn1ShaderPropertyType.Color;
					break;
				case UnityEngine.Rendering.ShaderPropertyType.Float:
					res = AllIn1ShaderPropertyType.Float;
					break;
				case UnityEngine.Rendering.ShaderPropertyType.Int:
					res = AllIn1ShaderPropertyType.Int;
					break;
				case UnityEngine.Rendering.ShaderPropertyType.Range:
					res = AllIn1ShaderPropertyType.Range;
					break;
				case UnityEngine.Rendering.ShaderPropertyType.Texture:
					res = AllIn1ShaderPropertyType.Texture;
					break;
				case UnityEngine.Rendering.ShaderPropertyType.Vector:
					res = AllIn1ShaderPropertyType.Vector;
					break;
				default:
					res = AllIn1ShaderPropertyType.Vector;
					break;
			}
#else
			switch (matProperty.type)
			{
				case MaterialProperty.PropType.Color:
					res = AllIn1ShaderPropertyType.Color;
					break;
				case MaterialProperty.PropType.Float:
					res = AllIn1ShaderPropertyType.Float;
					break;
				case MaterialProperty.PropType.Int:
					res = AllIn1ShaderPropertyType.Int;
					break;
				case MaterialProperty.PropType.Range:
					res = AllIn1ShaderPropertyType.Range;
					break;
				case MaterialProperty.PropType.Texture:
					res = AllIn1ShaderPropertyType.Texture;
					break;
				case MaterialProperty.PropType.Vector:
					res = AllIn1ShaderPropertyType.Vector;
					break;
				default:
					res = AllIn1ShaderPropertyType.Vector;
					break;
			}
#endif
			return res;

		}

		public static string ReadFileTextWithTabs(string path, int numTabs)
		{
			string res = string.Empty;

			string[] allLines = File.ReadAllLines(path);

			for (int i = 0; i < allLines.Length; i++)
			{
				string line = allLines[i];
				for (int j = 0; j < numTabs; j++)
				{
					line = "\t" + line;
				}
				line += "\n";

				res += line;
			}

			return res;
		}

		public static string UnifyEOL(string target)
		{
			string res = target;

			res = res.Replace("\r\n", "\n");
			res = res.Replace("\r", "\n");

			return res;
		}

		//Same name based match as AbstractPluginConfig.IsMaterialFromThisPlugin, for when only the shader is at hand
		public static bool IsShaderFromThisPlugin(Shader shader, AbstractPluginConfig pluginConfig)
		{
			if (shader == null || pluginConfig == null) return false;

			string[] shaderNames = pluginConfig.GetShaderNames();
			if (shaderNames == null) return false;

			for (int i = 0; i < shaderNames.Length; i++)
			{
				if (shader.name.Contains(shaderNames[i])) return true;
			}

			return false;
		}

		//Forward slashes and no trailing slash, the format Unity asset paths use on every platform.
		//Never returns null, so Path.Combine downstream cannot throw on a missing asmdef
		public static string NormalizeAssetPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			string res = path.Replace('\\', '/');

			//Unity stores canonical paths, so a doubled slash would break AssetPathToGUID lookups
			while (res.Contains("//"))
			{
				res = res.Replace("//", "/");
			}

			while (res.Length > 1 && res.EndsWith("/"))
			{
				res = res.Substring(0, res.Length - 1);
			}

			return res;
		}

		public static string CombineAssetPath(string parentFolder, string relativePath)
		{
			string normalizedParent = NormalizeAssetPath(parentFolder);
			string normalizedRelative = NormalizeAssetPath(relativePath);

			if (string.IsNullOrEmpty(normalizedParent))
			{
				return normalizedRelative;
			}

			if (string.IsNullOrEmpty(normalizedRelative))
			{
				return normalizedParent;
			}

			return normalizedParent + "/" + normalizedRelative.TrimStart('/');
		}

		public static string CombineAssetPath(params string[] segments)
		{
			string res = string.Empty;

			if (segments == null)
			{
				return res;
			}

			for (int i = 0; i < segments.Length; i++)
			{
				res = CombineAssetPath(res, segments[i]);
			}

			return res;
		}

		//Parent folder of an asset path. Path.GetDirectoryName is avoided here: it throws
		//ArgumentException on an empty path in Unity's Mono runtime, and returns backslashes on Windows
		public static string GetAssetParentFolder(string assetPath)
		{
			string normalizedPath = NormalizeAssetPath(assetPath);

			int lastSeparator = normalizedPath.LastIndexOf('/');
			if (lastSeparator < 0)
			{
				return string.Empty;
			}

			return normalizedPath.Substring(0, lastSeparator);
		}

		//Checks the disk, unlike AssetDatabase.IsValidFolder which also accepts backslashed paths
		public static bool AssetFolderExists(string assetFolderPath)
		{
			string normalizedPath = NormalizeAssetPath(assetFolderPath);

			if (string.IsNullOrEmpty(normalizedPath))
			{
				return false;
			}

			if (normalizedPath != "Assets" && !normalizedPath.StartsWith("Assets/"))
			{
				return false;
			}

			return Directory.Exists(normalizedPath);
		}

		//Creates missing parents too. False when the path is empty or outside Assets
		public static bool EnsureAssetFolderExists(string assetFolderPath)
		{
			string normalizedPath = NormalizeAssetPath(assetFolderPath);

			if (AssetFolderExists(normalizedPath))
			{
				return true;
			}

			if (string.IsNullOrEmpty(normalizedPath))
			{
				return false;
			}

			string[] folders = normalizedPath.Split('/');
			if (folders.Length == 0 || folders[0] != "Assets")
			{
				return false;
			}

			string currentPath = folders[0];
			for (int i = 1; i < folders.Length; i++)
			{
				if (string.IsNullOrEmpty(folders[i]))
				{
					continue;
				}

				string nextPath = currentPath + "/" + folders[i];

				//On disk but not imported yet would otherwise get a duplicate named "Folder 1"
				if (!Directory.Exists(nextPath) && !AssetDatabase.IsValidFolder(nextPath))
				{
					AssetDatabase.CreateFolder(currentPath, folders[i]);
				}

				currentPath = nextPath;
			}

			return Directory.Exists(currentPath);
		}

		public static void CreateFolderIfNotExist(string absoluteFolderPath)
		{
			//Debug, not AllIn1Logger, which is [Conditional("ALLIN1_LOG")] and vanishes for users
			if (!EnsureAssetFolderExists(absoluteFolderPath))
			{
				Debug.LogWarning($"[AllIn1] Could not create the folder '{absoluteFolderPath}'. " +
					"Check the Save Paths tab of the plugin window.");
			}
		}

		public static void DeleteAssetIfExist(string absoluteFolderPath)
		{
			AssetDatabase.DeleteAsset(absoluteFolderPath);
		}

		public static void DeleteFolderIfExist(string absoluteFolderPath)
		{
			if (AssetDatabase.IsValidFolder(absoluteFolderPath))
			{
				DeleteAssetIfExist(absoluteFolderPath);
			}
		}

		public static string GetFolderPathByFilePath(string filePath, int parentLevel = 0)
		{
			string res = GetAssetParentFolder(filePath);
			for (int i = 0; i < parentLevel; i++)
			{
				res = GetAssetParentFolder(res);
			}

			return res;
		}

		public static void ExecuteActionAfterInspectorUpdate(System.Action action)
		{
			EditorApplication.delayCall += () => 
			{ 
				if(action != null)
				{
					action(); 
				}
			};
		}
	}
}
