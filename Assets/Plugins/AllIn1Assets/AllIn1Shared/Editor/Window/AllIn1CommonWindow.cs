using System;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets
{
	public class AllIn1CommonWindow : EditorWindow
	{
		protected Vector2 scrollPosition;
		protected Texture imageInspector;
		protected AbstractPluginConfig pluginConfig;
		protected bool drawHeader;

		//A domain reload restores the window's serializable fields and drops everything else, so these
		//have to come back as false or Init is skipped while the styles and the tab drawers are null
		[NonSerialized] protected bool initialized;
		[NonSerialized] private bool initFailureReported;

		[NonSerialized] private CommonStyles commonStylesInstance;

		//CommonStyles cannot be serialized, so this rebuilds it instead of handing out a null
		protected CommonStyles commonStyles
		{
			get
			{
				if (commonStylesInstance == null)
				{
					commonStylesInstance = new CommonStyles();
				}

				return commonStylesInstance;
			}
		}

		//The registry is still empty while a package import or a configuration reset is in flight
		protected virtual AbstractPluginConfig FindPluginConfig()
		{
			return null;
		}

		protected virtual bool Init(AbstractPluginConfig pluginConfig, bool drawHeader = true)
		{
			this.pluginConfig = pluginConfig;

			if (pluginConfig == null) return false;

			if (imageInspector == null)
			{
				imageInspector = pluginConfig.GetInspectorImage();
			}

			return true;
		}

		//A destroyed config lands here too: a reset or a reimport replaces the asset while the window stays open
		protected void InitIfNeeded()
		{
			if (initialized && pluginConfig != null) return;

			try
			{
				initialized = Init(FindPluginConfig());

				if (initialized)
				{
					initFailureReported = false;
				}
			}
			catch (System.Exception e)
			{
				//This also runs from OnGUI, so anything a tab drawer throws while the assets are still
				//importing would come back on every repaint. Reported once, then retried silently
				initialized = false;

				if (!initFailureReported)
				{
					initFailureReported = true;
					Debug.LogWarning($"[AllIn1] {GetType().Name} could not open yet, it will retry. Details: {e}");
				}
			}
		}

		protected virtual void OnEnable()
		{
			InitIfNeeded();
		}

		protected void OnGUI()
		{
			InitIfNeeded();

			commonStyles.InitStylesIfNeeded();

			using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height)))
			{
				scrollPosition = scrollView.scrollPosition;

				if (!initialized)
				{
					DrawConfigurationNotReady();
				}
				else
				{
					DrawHeader();

					Draw();

					DrawFooter();
				}
			}
		}

		protected virtual void DrawHeader()
		{
			//Asked again rather than cached from Init: the config loads its image from an asset
			//postprocess, which can land after the window has already opened
			if (imageInspector == null && pluginConfig != null)
			{
				imageInspector = pluginConfig.GetInspectorImage();
			}

			if (imageInspector != null)
			{
				Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(50));
				GUI.DrawTexture(rect, imageInspector, ScaleMode.ScaleToFit, true);
			}

			EditorUtils.DrawThinLine();
		}

		//Drawing the tabs without a config used to throw a NullReferenceException on every repaint
		private void DrawConfigurationNotReady()
		{
			GUILayout.Label("Setting up the plugin", commonStyles.bigLabel);
			GUILayout.Space(20);
			GUILayout.Label("The plugin configuration is not ready yet. It is created once the assets finish importing, " +
				"so this clears on its own after the import or the reset that is running finishes.",
				commonStyles.wordWrappedStyle);
			GUILayout.Space(20);

			if (GUILayout.Button("Retry", GUILayout.MaxWidth(200)))
			{
				AssetDatabase.Refresh();
				InitIfNeeded();
			}
		}

		private void Draw()
		{
			if (Application.isPlaying)
			{
				DrawPlayMode();
			}
			else
			{
				DrawEditor();
			}
		}

		private void DrawPlayMode()
		{
			GUILayout.Label("Not available during play mode", commonStyles.bigLabel);
		}

		protected virtual void DrawEditor()
		{

		}

		private void DrawFooter()
		{
			GUILayout.Space(10);
			EditorUtils.DrawThinLine();
			GUILayout.Label("Current asset version is " + pluginConfig.GetVersion(), EditorStyles.boldLabel);
		}
	}
}
