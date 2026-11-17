using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public abstract class AllIn1Asset3DWindow : AllIn1CommonWindow
	{
		private int currTab = 0;

		//tabDrawers cannot survive a domain reload, so a restored tabsNames would disagree with it
		[System.NonSerialized] private string[] tabsNames;
		
		private AssetWindowTabDrawer CurrentTabDrawer
		{
			get
			{
				return tabDrawers[currTab];
			}
		}

		protected SavePathsTabDrawer savePathsTabDrawer;
		protected TextureEditorTabDrawer textureEditorTabDrawer;
		protected TextureCreatorTabDrawer textureCreatorTabDrawer;

		protected OverrideMaterialsTabDrawer overrideMaterialsTabDrawer;
		protected OtherTabDrawer otherTabDrawer;
		protected EffectsProfileTabDrawer effectsProfileTabDrawer;
		protected URPSettingsDrawer urpSettingsDrawer;

		protected Plugin3DConfig plugin3DConfig;

		protected AssetWindowTabDrawer[] tabDrawers;

		protected override bool Init(AbstractPluginConfig abstractPluginConfig, bool drawHeader = true)
		{
			if (!base.Init(abstractPluginConfig, drawHeader)) return false;

			this.plugin3DConfig = (Plugin3DConfig)abstractPluginConfig;

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

		protected virtual void CreateTabs()
		{
			savePathsTabDrawer = new SavePathsTabDrawer(commonStyles, this, plugin3DConfig);
			textureEditorTabDrawer = new TextureEditorTabDrawer(commonStyles, this, plugin3DConfig);
			textureCreatorTabDrawer = new TextureCreatorTabDrawer(commonStyles, this, plugin3DConfig);
			overrideMaterialsTabDrawer = new OverrideMaterialsTabDrawer(commonStyles, this, plugin3DConfig);
			otherTabDrawer = new OtherTabDrawer(commonStyles, this, plugin3DConfig);

			effectsProfileTabDrawer = new EffectsProfileTabDrawer(commonStyles, this, plugin3DConfig);
			urpSettingsDrawer = new URPSettingsDrawer(commonStyles, this, plugin3DConfig);
		}

		protected abstract void ConfigureTabs();

		protected override void DrawEditor()
		{
			base.DrawEditor();

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
	}
}