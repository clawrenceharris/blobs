using System.Collections.Generic;

namespace AllIn1Assets.SharedExtensions
{
	[System.Serializable]
	public class EffectsProfileGroup
	{
		public EffectGroupGlobalConfig effectGroupConfig;
		public List<EffectsProfileEntry> entries;

		public EffectsProfileGroup(EffectGroup effectGroup)
		{
			this.effectGroupConfig = effectGroup.effectGroupConfig;

			this.entries = new List<EffectsProfileEntry>();

			for(int i = 0; i < effectGroup.effects.Length; i++)
			{
				entries.Add(EffectsProfileEntry.Create(effectGroup.effects[i], true));
			}
		}

		public EffectsProfileGroup(EffectsProfileGroup copyFrom)
		{
			this.effectGroupConfig = copyFrom.effectGroupConfig;
			this.entries = new List<EffectsProfileEntry>(copyFrom.entries.Count);
			for(int i = 0; i < copyFrom.entries.Count; i++)
			{
				this.entries.Add(new EffectsProfileEntry(copyFrom.entries[i]));
			}
		}

		public void CollectKeywords(List<string> res)
		{
			for(int i = 0; i < entries.Count; i++)
			{
				if (entries[i].isEnabled)
				{
					entries[i].CollectKeywords(res);
				}
			}
		}

		public int FindEntryIndex(string effectID)
		{
			int res = -1;

			for (int i = 0; i < entries.Count; i++)
			{
				if (entries[i].effectID == effectID)
				{
					res = i;
					break;
				}
			}

			return res;
		}

		public bool HasEffectsEnabled()
		{
			bool res = false;

			for(int i = 0; i < entries.Count; i++)
			{
				if (entries[i].isEnabled)
				{
					res = true;
					break;
				}
			}

			return res;
		}
	}
}