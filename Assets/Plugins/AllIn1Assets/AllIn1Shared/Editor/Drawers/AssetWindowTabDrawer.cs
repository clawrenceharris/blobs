namespace AllIn1Assets
{
	public abstract class AssetWindowTabDrawer
	{
		protected CommonStyles commonStyles;
		protected AllIn1CommonWindow parentWindow;

		protected AbstractPluginConfig abstractPluginConfig;

		public AssetWindowTabDrawer(CommonStyles commonStyles, AllIn1CommonWindow parentWindow, AbstractPluginConfig abstractPluginConfig)
		{
			this.commonStyles = commonStyles;
			this.parentWindow = parentWindow;
			this.abstractPluginConfig = abstractPluginConfig;
		}

		public abstract void Hide();

		public abstract void Show();
		
		public abstract void OnDisable();
		
		public abstract void OnEnable();

		public abstract void Draw();

		public abstract void EnteredPlayMode();

		public virtual void ExitingEditMode()
		{
		
		}

		public abstract string GetTabName();
	}
}