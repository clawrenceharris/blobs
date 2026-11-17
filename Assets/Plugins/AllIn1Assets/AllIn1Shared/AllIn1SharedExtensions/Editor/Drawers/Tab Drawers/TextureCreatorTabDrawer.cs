namespace AllIn1Assets.SharedExtensions
{
	public class TextureCreatorTabDrawer : AssetWindowTabDrawer
	{
		private const string TAB_NAME = "Texture Creators";

		protected Plugin3DConfig plugin3DConfig;

		private NormalMapCreatorTool normalMapCreatorTool;
		private NormalMapCreatorDrawer normalMapCreatorDrawer;

		private GradientCreatorTool gradientCreatorTool;
		private GradientCreatorDrawer gradientCreatorDrawer;

		private AtlasPackerTool atlasPackerTool;
		private AtlasPackerDrawer atlasPackerDrawer;

		private NoiseCreatorTool noiseCreatorTool;
		private NoiseCreatorDrawer noiseCreatorDrawer;

		private RGBAPackerTool rgbaPackerTool;
		private RGBAPackerDrawer rgbaPackerDrawer;

		public TextureCreatorTabDrawer(CommonStyles commonStyles, AllIn1CommonWindow parentWindow, Plugin3DConfig plugin3DConfig) : base(commonStyles, parentWindow, plugin3DConfig)
		{
			this.plugin3DConfig = plugin3DConfig;

			Initialize();
		}

		private void Initialize()
		{
			normalMapCreatorTool = new NormalMapCreatorTool();
			normalMapCreatorDrawer = new NormalMapCreatorDrawer(normalMapCreatorTool, commonStyles, Repaint, plugin3DConfig);

			gradientCreatorTool = new GradientCreatorTool();
			gradientCreatorDrawer = new GradientCreatorDrawer(gradientCreatorTool, commonStyles, plugin3DConfig);

			atlasPackerTool = new AtlasPackerTool();
			atlasPackerDrawer = new AtlasPackerDrawer(atlasPackerTool, commonStyles, plugin3DConfig);

			noiseCreatorTool = new NoiseCreatorTool();
			noiseCreatorDrawer = new NoiseCreatorDrawer(noiseCreatorTool, commonStyles, plugin3DConfig);

			rgbaPackerTool = new RGBAPackerTool();
			rgbaPackerDrawer = new RGBAPackerDrawer(rgbaPackerTool, commonStyles, plugin3DConfig);
		}

		public override void OnEnable()
		{
		
		}

		public override void OnDisable()
		{
		
		}

		public override void Show()
		{
			Initialize();
		}

		public override void Hide()
		{
			
		}

		public override void EnteredPlayMode()
		{

		}

		public override void Draw()
		{
			normalMapCreatorDrawer.Draw();

			EditorUtils.DrawThinLine();

			gradientCreatorDrawer.Draw();

			EditorUtils.DrawThinLine();

			atlasPackerDrawer.Draw();

			EditorUtils.DrawThinLine();

			noiseCreatorDrawer.Draw();

			EditorUtils.DrawThinLine();

			rgbaPackerDrawer.Draw();
		}

		private void Repaint()
		{
			parentWindow.Repaint();
		}

		public override string GetTabName()
		{
			return TAB_NAME;
		}
	}
}