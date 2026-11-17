namespace AllIn1Assets.SharedExtensions
{
	[System.Serializable]
	public struct EffectKeywordData
	{
		public string keyword;
		public string displayName;

		public EffectKeywordData(string keyword, string displayName)
		{
			this.keyword = keyword;
			this.displayName = displayName;
		}
	}
}