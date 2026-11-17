using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class GlobalPropertiesDrawer
	{
		private List<int> globalPropertiesIndices;
		private Plugin3DMaterialInspectorReferences references;

		public GlobalPropertiesDrawer()
		{
		}

		public void Draw(List<int> globalPropertiesIndices, Plugin3DMaterialInspectorReferences references)
		{
			this.globalPropertiesIndices = globalPropertiesIndices;
			this.references = references;

			GUILayout.Label("Global Properties", references.bigLabelStyle);

			EditorGUILayout.BeginVertical(references.propertiesStyle);
			for (int i = 0; i < globalPropertiesIndices.Count; i++)
			{
				MaterialProperty matProperty = references.matProperties[globalPropertiesIndices[i]];

				EffectPropertyDrawer.DrawProperty(
					materialProperty: matProperty,
					labelPrefix: string.Empty,
					displayName: matProperty.displayName,
					customValue: string.Empty,
					allowReset: true,
					isKeywordProperty: false,
					propertyType: EffectProperty.PropertyType.BASIC,
					references: references);
			}

			EditorGUILayout.EndVertical();
		}
	}
}