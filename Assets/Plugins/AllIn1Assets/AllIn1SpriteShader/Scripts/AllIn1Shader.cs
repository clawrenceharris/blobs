using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace AllIn1Assets.AllIn1SpriteShader
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("AllIn1SpriteShader/AddAllIn1Shader")]
    public class AllIn1Shader : MonoBehaviour
    {
        public enum ShaderTypes
        {
            Default = 0,
            ScaledTime = 1,
            MaskedUI = 2,
            Urp2dRenderer = 3,
			Lit = 5,
			SRPBatcher = 6,
            Invalid = 4,
            LitTransparent = 7,
            MaskedUIScaledTime = 8,
        }
        public ShaderTypes currentShaderType = ShaderTypes.Invalid;

        public const string PREF_DEFAULT_SHADER = "allIn1DefaultShader";
        public const string PREF_RENDER_IMAGES_SCALE = "All1ShaderRenderImagesScale";

        //One mapping for every path that assigns a variant. Four copies of this chain had drifted apart
        public static string GetShaderName(ShaderTypes shaderType)
        {
            switch (shaderType)
            {
                case ShaderTypes.ScaledTime: return "AllIn1SpriteShaderScaledTime";
                case ShaderTypes.MaskedUI: return "AllIn1SpriteShaderUiMask";
                case ShaderTypes.Urp2dRenderer: return "AllIn1Urp2dRenderer";
                case ShaderTypes.Lit: return "AllIn1SpriteShaderLit";
                case ShaderTypes.SRPBatcher: return "AllIn1SpriteShaderSRPBatch";
                case ShaderTypes.LitTransparent: return "AllIn1SpriteShaderLitTransparent";
                case ShaderTypes.MaskedUIScaledTime: return "AllIn1SpriteShaderUiMaskScaledTime";
                default: return "AllIn1SpriteShader";
            }
        }

        public Material currMaterial, prevMaterial;
		public string subPath;
		public bool destroyed = false;
#if UNITY_EDITOR
		public bool matAssigned = false;
#endif
		public enum AfterSetAction { Clear, CopyMaterial, Reset};

        [Range(1f, 20f)] public float normalStrength = 5f;
        [Range(0f, 3f)] public int normalSmoothing = 1;
        [HideInInspector] public bool computingNormal = false;

#if UNITY_EDITOR
        //private static float timeLastReload = -1f;
        //private void Start()
        //{
        //    if(timeLastReload < 0) timeLastReload = Time.time;
        //}

#endif

        public void ClearAllKeywords()
        {
            SetKeyword("RECTSIZE_ON");
            SetKeyword("OFFSETUV_ON");
            SetKeyword("CLIPPING_ON");
            SetKeyword("POLARUV_ON");
            SetKeyword("TWISTUV_ON");
            SetKeyword("ROTATEUV_ON");
            SetKeyword("FISHEYE_ON");
            SetKeyword("PINCH_ON");
            SetKeyword("SHAKEUV_ON");
            SetKeyword("WAVEUV_ON");
            SetKeyword("ROUNDWAVEUV_ON");
            SetKeyword("DOODLE_ON");
            SetKeyword("ZOOMUV_ON");
            SetKeyword("FADE_ON");
            SetKeyword("TEXTURESCROLL_ON");
            SetKeyword("GLOW_ON");
            SetKeyword("OUTBASE_ON");
            SetKeyword("ONLYOUTLINE_ON");
            SetKeyword("OUTTEX_ON");
            SetKeyword("OUTDIST_ON");
            SetKeyword("DISTORT_ON");
            SetKeyword("WIND_ON");
            SetKeyword("GRADIENT_ON");
            SetKeyword("GRADIENT2COL_ON");
            SetKeyword("RADIALGRADIENT_ON");
            SetKeyword("COLORSWAP_ON");
            SetKeyword("HSV_ON");
            SetKeyword("HITEFFECT_ON");
            SetKeyword("PIXELATE_ON");
            SetKeyword("NEGATIVE_ON");
            SetKeyword("GRADIENTCOLORRAMP_ON");
            SetKeyword("COLORRAMP_ON");
            SetKeyword("GREYSCALE_ON");
            SetKeyword("POSTERIZE_ON");
            SetKeyword("BLUR_ON");
            SetKeyword("MOTIONBLUR_ON");
            SetKeyword("GHOST_ON");
            SetKeyword("ALPHAOUTLINE_ON");
            SetKeyword("INNEROUTLINE_ON");
            SetKeyword("ONLYINNEROUTLINE_ON");
            SetKeyword("HOLOGRAM_ON");
            SetKeyword("CHROMABERR_ON");
            SetKeyword("GLITCH_ON");
            SetKeyword("FLICKER_ON");
            SetKeyword("SHADOW_ON");
            SetKeyword("SHINE_ON");
            SetKeyword("CONTRAST_ON");
            SetKeyword("OVERLAY_ON");
            SetKeyword("OVERLAYMULT_ON");
            SetKeyword("ALPHACUTOFF_ON");
            SetKeyword("ALPHAROUND_ON");
            SetKeyword("CHANGECOLOR_ON");
            SetKeyword("CHANGECOLOR2_ON");
            SetKeyword("CHANGECOLOR3_ON");
            SetKeyword("FOG_ON");

            /*SetSceneDirty();*/
        }

        public void SetKeyword(string keyword, bool state = false)
        {
            if (destroyed) return;
            if (currMaterial == null)
            {
                FindCurrMaterial();
                if (currMaterial == null)
                {
                    MissingRenderer();
                    return;
                }
            }
            if (!state) currMaterial.DisableKeyword(keyword);
            else currMaterial.EnableKeyword(keyword);
        }

        public void FindCurrMaterial()
        {
            Renderer sr = GetComponent<Renderer>();
            if (sr != null)
            {
                currMaterial = GetComponent<Renderer>().sharedMaterial;
#if UNITY_EDITOR
                matAssigned = true;
#endif
            }
            else
            {
                Graphic img = GetComponent<Graphic>();
                if (img != null)
                {
                    currMaterial = img.material;
#if UNITY_EDITOR
                    matAssigned = true;
#endif
                }
            }
        }

        public void CleanMaterial()
        {
            Renderer sr = GetComponent<Renderer>();
            if (sr != null)
            {
                sr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
#if UNITY_EDITOR
                matAssigned = false;
#endif
            }
            else
            {
                Graphic img = GetComponent<Graphic>();
                if (img != null)
                {
                    img.material = new Material(Shader.Find("Sprites/Default"));
#if UNITY_EDITOR
                    matAssigned = false;
#endif
                }
            }

            /*SetSceneDirty();*/
        }

//        public bool SaveMaterial()
//        {
//#if UNITY_EDITOR
//            string sameMaterialPath = AllIn1ShaderWindow.GetMaterialSavePath();
//            sameMaterialPath += "/";
//            if (!System.IO.Directory.Exists(sameMaterialPath))
//            {
//                EditorUtility.DisplayDialog("The desired Material Save Path doesn't exist",
//                    "Go to Window -> AllIn1ShaderWindow and set a valid folder", "Ok");
//                return false;
//            }
//            sameMaterialPath += gameObject.name;
//            string fullPath = sameMaterialPath + ".mat";
//            if (System.IO.File.Exists(fullPath))
//            {
//                SaveMaterialWithOtherName(sameMaterialPath);
//            }
//            else DoSaving(fullPath);
//            SetSceneDirty();
//            return true;
//#else
//            return false;
//#endif
//        }
        public void SaveMaterialWithOtherName(string path, int i = 1)
        {
            int number = i;
            string newPath = path + "_" + number.ToString();
            string fullPath = newPath + ".mat";
            if (System.IO.File.Exists(fullPath))
            {
                number++;
                SaveMaterialWithOtherName(path, number);
            }
            else
            {
                DoSaving(fullPath);
            }
        }

        public void DoSaving(string fileName)
        {
#if UNITY_EDITOR
            bool rendererExists = false;
            Renderer sr = GetComponent<Renderer>();
            Material matToSave = null;
            Material createdMat = null;
            if (sr != null)
            {
                rendererExists = true;
                matToSave = sr.sharedMaterial;
            }
            else
            {
                Graphic img = GetComponent<Graphic>();
                if (img != null)
                {
                    rendererExists = true;
                    matToSave = img.material;
                }
            }
            if (!rendererExists)
            {
                MissingRenderer();
                return;
            }
            else
            {
                createdMat = new Material(matToSave);
                currMaterial = createdMat;
                AssetDatabase.CreateAsset(createdMat, fileName);
                Debug.Log(fileName + " has been saved!");
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(fileName, typeof(Material)));
            }
            if (sr != null)
            {
                sr.material = createdMat;
            }
            else
            {
                Graphic img = GetComponent<Graphic>();
                img.material = createdMat;
            }
#endif
        }


        public void MissingRenderer()
        {
#if UNITY_EDITOR
            EditorUtility.DisplayDialog("Missing Renderer", "This GameObject (" +
                                                            gameObject.name + ") has no Renderer or UI Image component. This AllIn1Shader component will be removed.", "Ok");
            destroyed = true;
            DestroyImmediate(this);
#endif
        }

        public bool ToggleSetAtlasUvs(bool activate)
        {
            bool success = false;
            SetAtlasUvs atlasUvs = GetComponent<SetAtlasUvs>();
            if (activate)
            {
                if (atlasUvs == null) atlasUvs = gameObject.AddComponent<SetAtlasUvs>();
                if (atlasUvs != null) success = atlasUvs.GetAndSetUVs();
                if(success) SetKeyword("ATLAS_ON", true);
            }
            else
            {
                if (atlasUvs != null)
                {
                    atlasUvs.ResetAtlasUvs();
                    DestroyImmediate(atlasUvs);
                    success = true;
                }
                else
                {
                    #if UNITY_EDITOR
                    EditorUtility.DisplayDialog("Missing Atlas Uv Setup", "This GameObject (" + gameObject.name + ") has no Atlas Uv Setup.", "Ok");
                    #endif
                    return false;
                }
                SetKeyword("ATLAS_ON", false);
            }

            /*SetSceneDirty();*/
            return success;
        }

        public bool ApplyMaterialToHierarchy()
        {
            Renderer sr = GetComponent<Renderer>();
            Graphic image = GetComponent<Graphic>();
            Material matToApply = null;
            if (sr != null) matToApply = sr.sharedMaterial;
            else if (image != null)
            {
                matToApply = image.material;
            }
            else
            {
                MissingRenderer();
                return false;
            }

            List<Transform> children = new List<Transform>();
            GetAllChildren(transform, ref children);
            bool hasPerformedOperation = false;
            foreach (Transform t in children)
            {
                sr = t.gameObject.GetComponent<Renderer>();
                if (sr != null) sr.material = matToApply;
                else
                {
                    image = t.gameObject.GetComponent<Graphic>();
                    if (image != null) image.material = matToApply;
                }
                hasPerformedOperation = true;
            }

            return hasPerformedOperation;
        }

        public void CheckIfValidTarget()
        {
            Renderer sr = GetComponent<Renderer>();
            Graphic image = GetComponent<Graphic>();
            if (sr == null && image == null) MissingRenderer();
        }

        private void GetAllChildren(Transform parent, ref List<Transform> transforms)
        {
            foreach (Transform child in parent)
            {
                transforms.Add(child);
                GetAllChildren(child, ref transforms);
            }
        }

		public static void ShowNotification(string message, double fadeOutWait = 1.0)
		{
#if UNITY_EDITOR
			if (SceneView.lastActiveSceneView == null) return;
			SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message), fadeOutWait);
#endif
		}

		#region normalMapCreator
//		protected virtual void OnEnable()
//        {
//#if UNITY_EDITOR
//            EditorApplication.update += OnEditorUpdate;
//#endif
//        }

//        protected virtual void OnDisable()
//        {
//#if UNITY_EDITOR
//            EditorApplication.update -= OnEditorUpdate;
//#endif
//        }

        public bool needToWait;
        public int waitingCycles;
        public int timesWeWaited;

        public SpriteRenderer normalMapSr;
		public Renderer normalMapRenderer;
        public bool isSpriteRenderer;
        public void CreateAndAssignNormalMap(string normalMapSavePath)
        {
#if UNITY_EDITOR
            if (GetComponent<TilemapRenderer>() != null)
            {
                EditorUtility.DisplayDialog("This is a tilemap", "This feature isn't supported on Tilemap Renderers." +
                                                                 " Add a secondary normal map texture instead (you can create a Normal Map in the asset Window)", "Ok");
                return;
            }

            normalMapSr = GetComponent<SpriteRenderer>();
            normalMapRenderer = GetComponent<Renderer>();
            //Debug.LogError($"NORMALMAP_ON: {normalMapRenderer.sharedMaterial.IsKeywordEnabled("NORMALMAP_ON")}  -t:{Time.time}");
            if (normalMapSr != null)
            {
                isSpriteRenderer = true;
                SetNewNormalTexture(normalMapSavePath);
                if(!normalMapSr.sharedMaterial.IsKeywordEnabled("NORMALMAP_ON")) normalMapSr.sharedMaterial.EnableKeyword("NORMALMAP_ON");
            }
            else if (normalMapRenderer != null)
            {
                isSpriteRenderer = false;
                SetNewNormalTexture(normalMapSavePath);
                if(!normalMapRenderer.sharedMaterial.IsKeywordEnabled("NORMALMAP_ON")) normalMapRenderer.sharedMaterial.EnableKeyword("NORMALMAP_ON");
            }
            else
            {
                if (GetComponent<Graphic>() != null)
                {
                    EditorUtility.DisplayDialog("This is a UI element", "This GameObject (" +
                                                                        gameObject.name + ") is a UI element. UI elements probably shouldn't have a normal map. Why are you using the light shader variant?", "Ok");
                }
                else
                {
                    MissingRenderer();
                }
                return;
            }
#endif
        }

		public string path;
        private void SetNewNormalTexture(string normalMapSavePath)
        {
#if UNITY_EDITOR
			path = normalMapSavePath;
			/*path = AllIn1ShaderWindow.GetNormalMapSavePath();*/
			path += "/";
            if (!System.IO.Directory.Exists(path))
            {
                EditorUtility.DisplayDialog("The desired folder doesn't exist",
                    "Go to Tools -> AllIn1 -> Sprite Shader Window and set a valid folder", "Ok");
                return;
            }
            
            computingNormal = true;
            needToWait = true;
            waitingCycles = 0;
            timesWeWaited = 0;
#else
            computingNormal = false;
            return;
#endif
        }

#if UNITY_EDITOR
        public TextureImporter importer;
		public Texture2D mainTex2D;
#endif

		#endregion
    }
}