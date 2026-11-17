using AllIn1Assets.SharedExtensions;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.AllIn1SpriteShader
{
    public class AllIn1ShaderWindow : AllIn1CommonWindow
	{
        [MenuItem("Tools/AllIn1/Sprite Shader Window", priority = 3)]
        public static void ShowAllIn1ShaderWindowWindow()
        {
            GetWindow<AllIn1ShaderWindow>("Sprite Shader Window");
        }
       
		private int currTab = 0;

		//tabDrawers cannot survive a domain reload, so a restored tabsNames would disagree with it
		[System.NonSerialized] private string[] tabsNames;

		protected Plugin3DConfig plugin3DConfig;
		private AssetWindowTabDrawer CurrentTabDrawer => tabDrawers[currTab];
		protected SavePathsAllIn1SpriteTabDrawer savePathsTabDrawer;
		protected TextureCreatorTabDrawer textureCreatorTabDrawer;
		protected OthersAllIn1SpriteTabDrawer othersTabDrawer;
		protected AssetWindowTabDrawer[] tabDrawers;

    
        public enum ImageType
        {
            ShowImage,
            HideInComponent,
            HideEverywhere
        }


		protected override AbstractPluginConfig FindPluginConfig()
		{
			return PluginRegistry.GetPluginByID<AllIn1SpriteShaderConfig>(PluginID.ALLIN1_SPRITE);
		}

		protected override bool Init(AbstractPluginConfig pluginConfig, bool drawHeader = true)
		{
			if (!base.Init(pluginConfig, drawHeader)) return false;

			this.plugin3DConfig = (Plugin3DConfig)pluginConfig;

			CreateTabs();
			ConfigureTabs();

			tabsNames = new string[tabDrawers.Length];
			for (int i = 0; i < tabsNames.Length; i++)
			{
				tabsNames[i] = tabDrawers[i].GetTabName();
			}

			currTab = Mathf.Clamp(currTab, 0, tabDrawers.Length - 1);

			return true;
		}

		private void CreateTabs()
		{
			savePathsTabDrawer = new SavePathsAllIn1SpriteTabDrawer(commonStyles, this, plugin3DConfig);
			textureCreatorTabDrawer = new TextureCreatorTabDrawer(commonStyles, this, plugin3DConfig);
			othersTabDrawer = new OthersAllIn1SpriteTabDrawer(commonStyles, this, plugin3DConfig);
		}

		private void ConfigureTabs()
		{
			tabDrawers = new AssetWindowTabDrawer[]
			{
				savePathsTabDrawer,
				textureCreatorTabDrawer,
				othersTabDrawer
			};
		}

		protected override void DrawHeader()
		{
			int allIn1ImageConfigInt = EditorPrefs.GetInt("allIn1ImageConfig", 0);
			ImageType imageType = (ImageType)allIn1ImageConfigInt;
			this.drawHeader = (imageType == ImageType.ShowImage) || (imageType == ImageType.HideInComponent);

			if (drawHeader)
			{
				base.DrawHeader();
			}
		}

		protected override void DrawEditor()
		{
			base.DrawEditor();


			int allIn1ImageConfigInt = EditorPrefs.GetInt("allIn1ImageConfig", 0);
			ImageType imageType = (ImageType)allIn1ImageConfigInt;
			this.drawHeader = (imageType == ImageType.ShowImage) || (imageType == ImageType.HideInComponent);

			int newTab = GUILayout.Toolbar(currTab, tabsNames);
			AssetWindowTabDrawer newTabDrawer = tabDrawers[newTab];

			if (newTabDrawer != CurrentTabDrawer)
			{
				CurrentTabDrawer.Hide();
				newTabDrawer.Show();

				this.currTab = newTab;
			}

			EditorUtils.DrawThinLine();
			CurrentTabDrawer.Draw();
		}

        private static string GetNewValidPath(string path, string extension = ".png", int i = 1)
        {
            int number = i;
            path = path.Replace(extension, "");
            string newPath = path + "_" + number.ToString();
            string fullPath = newPath + extension;
            if(File.Exists(fullPath))
            {
                number++;
                fullPath = GetNewValidPath(path, extension, number);
            }

            return fullPath;
        }

        public static Texture2D CreateNormalMap(Texture2D t, float normalMult = 5f, int normalSmooth = 0)
        {
            int width = t.width;
            int height = t.height;
            Color[] sourcePixels = t.GetPixels();
            Color[] resultPixels = new Color[width * height];
            Vector3 vScale = new Vector3(0.3333f, 0.3333f, 0.3333f);

            for(int y = 0; y < height; y++)
            {
                for(int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    Vector3 cSampleNegXNegY = GetPixelClamped(sourcePixels, x - 1, y - 1, width, height);
                    Vector3 cSampleZerXNegY = GetPixelClamped(sourcePixels, x, y - 1, width, height);
                    Vector3 cSamplePosXNegY = GetPixelClamped(sourcePixels, x + 1, y - 1, width, height);
                    Vector3 cSampleNegXZerY = GetPixelClamped(sourcePixels, x - 1, y, width, height);
                    Vector3 cSamplePosXZerY = GetPixelClamped(sourcePixels, x + 1, y, width, height);
                    Vector3 cSampleNegXPosY = GetPixelClamped(sourcePixels, x - 1, y + 1, width, height);
                    Vector3 cSampleZerXPosY = GetPixelClamped(sourcePixels, x, y + 1, width, height);
                    Vector3 cSamplePosXPosY = GetPixelClamped(sourcePixels, x + 1, y + 1, width, height);

                    float fSampleNegXNegY = Vector3.Dot(cSampleNegXNegY, vScale);
                    float fSampleZerXNegY = Vector3.Dot(cSampleZerXNegY, vScale);
                    float fSamplePosXNegY = Vector3.Dot(cSamplePosXNegY, vScale);
                    float fSampleNegXZerY = Vector3.Dot(cSampleNegXZerY, vScale);
                    float fSamplePosXZerY = Vector3.Dot(cSamplePosXZerY, vScale);
                    float fSampleNegXPosY = Vector3.Dot(cSampleNegXPosY, vScale);
                    float fSampleZerXPosY = Vector3.Dot(cSampleZerXPosY, vScale);
                    float fSamplePosXPosY = Vector3.Dot(cSamplePosXPosY, vScale);

                    float edgeX = (fSampleNegXNegY - fSamplePosXNegY) * 0.25f + (fSampleNegXZerY - fSamplePosXZerY) * 0.5f + (fSampleNegXPosY - fSamplePosXPosY) * 0.25f;
                    float edgeY = (fSampleNegXNegY - fSampleNegXPosY) * 0.25f + (fSampleZerXNegY - fSampleZerXPosY) * 0.5f + (fSamplePosXNegY - fSamplePosXPosY) * 0.25f;

                    Vector2 vEdge = new Vector2(edgeX, edgeY) * normalMult;
                    Vector3 norm = new Vector3(vEdge.x, vEdge.y, 1.0f).normalized;
                    resultPixels[index] = new Color(norm.x * 0.5f + 0.5f, norm.y * 0.5f + 0.5f, norm.z * 0.5f + 0.5f, 1);
                }
            }

            if(normalSmooth > 0)
            {
                resultPixels = SmoothNormals(resultPixels, width, height, normalSmooth);
            }

            Texture2D texNormal = new Texture2D(width, height, TextureFormat.RGB24, false, false);
            texNormal.SetPixels(resultPixels);
            texNormal.Apply();
            return texNormal;
        }

        private static Vector3 GetPixelClamped(Color[] pixels, int x, int y, int width, int height)
        {
            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            Color c = pixels[x + y * width];
            return new Vector3(c.r, c.g, c.b);
        }

        private static Color[] SmoothNormals(Color[] pixels, int width, int height, int normalSmooth)
        {
            Color[] smoothedPixels = new Color[pixels.Length];
            float step = 0.00390625f * normalSmooth;

            for(int y = 0; y < height; y++)
            {
                for(int x = 0; x < width; x++)
                {
                    float pixelsToAverage = 0.0f;
                    Color c = pixels[x + y * width];
                    pixelsToAverage++;

                    for(int offsetY = -normalSmooth; offsetY <= normalSmooth; offsetY++)
                    {
                        for(int offsetX = -normalSmooth; offsetX <= normalSmooth; offsetX++)
                        {
                            if(offsetX == 0 && offsetY == 0) continue;

                            int sampleX = Mathf.Clamp(x + offsetX, 0, width - 1);
                            int sampleY = Mathf.Clamp(y + offsetY, 0, height - 1);

                            c += pixels[sampleX + sampleY * width];
                            pixelsToAverage++;
                        }
                    }

                    smoothedPixels[x + y * width] = c / pixelsToAverage;
                }
            }

            return smoothedPixels;
        }
        
        [MenuItem("Assets/Create/AllIn1Shader Materials/CreateDefaultMaterial")]
        public static void CreateDefaultMaterial()
        {
            CreateMaterial("AllIn1SpriteShader");
        }
        
        [MenuItem("Assets/Create/AllIn1Shader Materials/CreateScaledTimeMaterial")]
        public static void CreateScaledTimeMaterial()
        {
            CreateMaterial("AllIn1SpriteShaderScaledTime");
        }
        
        [MenuItem("Assets/Create/AllIn1Shader Materials/CreateUiMaskMaterial")]
        public static void CreateUiMaskMaterial()
        {
            CreateMaterial("AllIn1SpriteShaderUiMask");
        }

        private static void CreateMaterial(string shaderName)
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            if(!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
            {
                Material material = new Material(FindShader(shaderName));
                string fullPath = selectedPath + "/Mat-" + shaderName + ".mat";
                if(File.Exists(fullPath)) fullPath = GetNewValidPath(fullPath, ".mat");
                AssetDatabase.CreateAsset(material, fullPath);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("Please select a valid folder in the Project Window.");
            }
        }
       
		public static void SceneViewNotificationAndLog(string message)
        {
            Debug.Log(message);
            ShowSceneViewNotification(message);
        }

        public static void ShowSceneViewNotification(string message)
        {
            bool showNotification = EditorPrefs.GetInt("DisplaySceneViewNotifications", 1) == 1;
            if(!showNotification || SceneView.lastActiveSceneView == null) return;

            GUIContent content = new GUIContent(message);
            SceneView.lastActiveSceneView.ShowNotification(content, 1.5f);
        }
        
        public static Shader FindShader(string shaderName)
        {
            string[] guids = AssetDatabase.FindAssets($"{shaderName} t:shader");
            foreach(string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if(shader != null)
                {
                    string fullShaderName = shader.name;
                    string actualShaderName = fullShaderName.Substring(fullShaderName.LastIndexOf('/') + 1);
                    if(actualShaderName.Equals(shaderName)) return shader;
                }
            }
            return null;
        }
    }
}