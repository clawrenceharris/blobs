using System;
using UnityEngine;

namespace AllIn1Assets
{
	public class DemoElement : MonoBehaviour
	{
		public string label;
		public DemoElementUI demoElementUI;

		public void Init()
		{
			if(demoElementUI != null)
			{
				demoElementUI.Init(this);
			}
		}

		private void OnValidate()
		{
			if(demoElementUI != null)
			{
				demoElementUI.UpdateGUI(this);
			}
		}
	}
}