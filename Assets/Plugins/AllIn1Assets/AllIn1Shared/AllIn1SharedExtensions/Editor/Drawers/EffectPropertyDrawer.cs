using AllIn1Assets;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public static class EffectPropertyDrawer
	{
		public static void DrawMainProperty(int globalEffectIndex, 
			AllIn13DEffectConfig effectConfig, Plugin3DMaterialInspectorReferences references, 
			bool isEffectAvailable, bool isEffectAvailableOnGlobalConfig)
		{
			EditorGUILayout.BeginHorizontal();

			bool isShaderVariant = references.IsShaderVariant();
			string label = $"{globalEffectIndex}. {effectConfig.displayName}";

			if (isEffectAvailableOnGlobalConfig || isShaderVariant)
			{
				switch (effectConfig.effectConfigType)
				{
					case EffectConfigType.EFFECT_TOGGLE:
						DrawMainPropertyToggle(label, effectConfig, references, isShaderVariant);
						break;
					case EffectConfigType.EFFECT_ENUM:
						DrawMainPropertyEnum(label, effectConfig, references, isEffectAvailable, isShaderVariant);
						break;
				}
			}
			else
			{
				int keywordSelectedIndex = 0;
				references.IsEffectEnabled(effectConfig, ref keywordSelectedIndex);

				GUIContent guiContent = effectConfig.CreateGUIContent(globalEffectIndex, keywordSelectedIndex);
				EditorGUILayout.LabelField(guiContent, GUILayout.Width(180f));
				PropertyNameCopyMenu.HandleLast(PropertyNameCopyMenu.KEYWORD, effectConfig.keywords[keywordSelectedIndex].keyword);
				EditorGUILayout.LabelField("Disabled in Active Effects List");
			}
			
			EditorGUILayout.EndHorizontal();
		}

		public static void DrawMainPropertyToggle(string label, 
			AllIn13DEffectConfig effectConfig, Plugin3DMaterialInspectorReferences references,
			bool isShaderVariant)
		{
			bool isEffectEnabled = AllIn13DEffectConfig.IsEffectEnabled(effectConfig, references);
			
			EditorGUI.BeginChangeCheck();

			string keyword = effectConfig.keywords[0].keyword;
			GUIContent guiContent = new GUIContent(label, PropertyNameCopyMenu.Tooltip(keyword));

			bool effectEnabledInAllMaterials = true;
			bool effectDisabledInAllMaterials = true;
			bool allMaterialsAreVariants = true;
			for (int i = 0; i < references.targetMatInfos.Length; i++)
			{
				bool effectEnabledThisMat = AllIn13DEffectConfig.IsEffectEnabled(effectConfig, references.targetMatInfos[i]);
				
				effectEnabledInAllMaterials = effectEnabledInAllMaterials && effectEnabledThisMat;
				effectDisabledInAllMaterials = effectDisabledInAllMaterials && !effectEnabledThisMat;
				allMaterialsAreVariants = allMaterialsAreVariants && references.targetMatInfos[i].IsShaderVariant();
			}

			if (allMaterialsAreVariants)
			{
				GUILayout.Label(guiContent);
				PropertyNameCopyMenu.HandleLast(PropertyNameCopyMenu.KEYWORD, keyword);
			}
			else
			{
				bool mixedValue = (!(effectEnabledInAllMaterials || effectDisabledInAllMaterials)) && references.targetMatInfos.Length > 1;
				string style = mixedValue ? "ToggleMixed" : "Toggle";
				isEffectEnabled = PropertyNameCopyMenu.Toggle(isEffectEnabled, guiContent, style, PropertyNameCopyMenu.KEYWORD, keyword);
			}
			if (EditorGUI.EndChangeCheck())
			{
				for(int i = 0; i < references.targetMatInfos.Length; i++)
				{
					AbstractMaterialInfo matInfo = references.targetMatInfos[i];

					if (isEffectEnabled)
					{
						AllIn13DEffectConfig.EnableEffect(effectConfig, references, matInfo);
					}
					else
					{
						AllIn13DEffectConfig.DisableEffect(effectConfig, matInfo);
					}

					if (references.ClearsOptionKeywordsOfDisabledEffects)
					{
						AllIn13DEffectConfig.SetOptionKeywords(effectConfig, matInfo, isEffectEnabled);
					}
				}

				references.matProperties[effectConfig.keywordPropertyIndex].floatValue = isEffectEnabled ? 1f : 0f;

				EditorUtils.SetDirtyCurrentScene();

				references.SetMaterialsDirty();
			}
		}

		public static void DrawMainPropertyEnum(string label, 
			AllIn13DEffectConfig effectConfig, Plugin3DMaterialInspectorReferences references,
			bool isEffectAvailable, bool isShaderVariant)
		{
			int selectedIndex = 0;
			//bool isEffectEnabled = AllIn13DEffectConfig.IsEffectEnabled(effectConfig, ref selectedIndex, references);


			bool sameEnumValueInAllMaterials = references.IsEffectEnabled(effectConfig, ref selectedIndex);
			//bool sameEnumValueInAllMaterials = true;
			//for(int i = 0; i < references.targetMatInfos.Length; i++)
			//{
			//	int enumIdx = -1;
			//	AbstractMaterialInfo matInfo = references.targetMatInfos[i];

			//	bool effectEnabled = AllIn13DEffectConfig.IsEffectEnabled(effectConfig, ref enumIdx, matInfo);

			//	if(i == 0)
			//	{
			//		selectedIndex = enumIdx;
			//	}
			//	else
			//	{
			//		sameEnumValueInAllMaterials = sameEnumValueInAllMaterials && (enumIdx == selectedIndex);
			//	}
			//}




			string keyword = effectConfig.keywords[selectedIndex].keyword;
			GUIContent guiContent = new GUIContent(label, PropertyNameCopyMenu.Tooltip(keyword));


			if (isShaderVariant)
			{
				if (isEffectAvailable)
				{
					EditorGUILayout.BeginHorizontal(references.propertiesStyle);
					EditorGUILayout.LabelField(guiContent);
					PropertyNameCopyMenu.HandleLast(PropertyNameCopyMenu.KEYWORD, keyword);
					EditorGUILayout.LabelField(effectConfig.keywordsDisplayNames[selectedIndex]);
					EditorGUILayout.EndHorizontal();
				}
				else
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(guiContent);
					PropertyNameCopyMenu.HandleLast(PropertyNameCopyMenu.KEYWORD, keyword);
					EditorGUILayout.EndHorizontal();
				}
			}
			else
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.showMixedValue = !sameEnumValueInAllMaterials;
				Rect popupRow = PropertyNameCopyMenu.PopupRow();
				if (PropertyNameCopyMenu.IsMenuClick(PropertyNameCopyMenu.LabelRect(popupRow))) PropertyNameCopyMenu.ShowMenu(PropertyNameCopyMenu.KEYWORD, GetKeywordNames(effectConfig), selectedIndex);
				selectedIndex = EditorGUI.Popup(popupRow, guiContent, selectedIndex, PropertyNameCopyMenu.PopupContents(effectConfig.keywordsDisplayNames));
				EditorGUI.showMixedValue = false;

				if (EditorGUI.EndChangeCheck())
				{
					for (int i = 0; i < references.targetMatInfos.Length; i++)
					{
						AbstractMaterialInfo matInfo = references.targetMatInfos[i];

						if (selectedIndex >= 0)
						{
							AllIn13DEffectConfig.EnableEffectByIndex(effectConfig, selectedIndex, references, matInfo);
						}
						else
						{
							AllIn13DEffectConfig.DisableEffect(effectConfig, matInfo);
						}
					}

					references.matProperties[effectConfig.keywordPropertyIndex].floatValue = selectedIndex;

					EditorUtils.SetDirtyCurrentScene();

					references.SetMaterialsDirty();
				}
			}
		}

		//public static void DrawProperty(int propertyIndex, string labelPrefix, bool allowReset, bool isKeywordProperty, AllIn13DShaderInspectorReferences references)
		//{
		//	MaterialProperty targetProperty = references.matProperties[propertyIndex];
		//	DrawProperty(
		//		materialProperty: targetProperty,
		//		labelPrefix: labelPrefix,
		//		displayName: targetProperty.displayName,
		//		allowReset: allowReset,
		//		isKeywordProperty: isKeywordProperty,
		//		references: references);
		//}

		//public static void DrawProperty(EffectProperty effectProperty, string labelPrefix, bool allowReset, AllIn13DShaderInspectorReferences references)
		//{
		//	DrawProperty(effectProperty.propertyIndex, labelPrefix, effectProperty.allowReset, effectProperty.IsPropertyWithKeywords(), references);
		//}

		//public static void DrawProperty(EffectProperty effectProperty, AllIn13DShaderInspectorReferences references)
		//{
		//	DrawProperty(propertyIndex: effectProperty.propertyIndex, isKeywordProperty: effectProperty.IsPropertyWithKeywords(),  references: references);
		//}

		//public static void DrawProperty(int propertyIndex, bool isKeywordProperty, AllIn13DShaderInspectorReferences references)
		//{
		//	DrawProperty(propertyIndex, string.Empty, true, isKeywordProperty, references);
		//}

		//public static void DrawProperty(MaterialProperty materialProperty, AllIn13DShaderInspectorReferences references)
		//{
		//	DrawProperty(materialProperty, false, references);
		//}

		//public static void DrawProperty(MaterialProperty materialProperty, bool isKeywordProperty, AllIn13DShaderInspectorReferences references)
		//{
		//	DrawProperty(
		//		materialProperty: materialProperty, 
		//		labelPrefix: string.Empty, 
		//		displayName: materialProperty.displayName,
		//		allowReset: true, 
		//		isKeywordProperty: isKeywordProperty, 
		//		references: references);
		//}

		//public static void DrawProperty(MaterialProperty materialProperty, bool allowReset, bool isKeywordProperty, AllIn13DShaderInspectorReferences references)
		//{
		//	DrawProperty(
		//		materialProperty: materialProperty, 
		//		labelPrefix: string.Empty, 
		//		displayName: materialProperty.displayName,
		//		allowReset: allowReset, 
		//		isKeywordProperty: isKeywordProperty, 
		//		references: references);
		//}

		//public static void DrawProperty(
		//	MaterialProperty materialProperty, 
		//	string labelPrefix, 
		//	bool allowReset, 
		//	bool isKeywordProperty, 
		//	AllIn13DShaderInspectorReferences references)
		//{
		//	DrawProperty(
		//		materialProperty: materialProperty, 
		//		labelPrefix: labelPrefix, 
		//		displayName: materialProperty.displayName,
		//		allowReset: allowReset, 
		//		isKeywordProperty: isKeywordProperty,
		//		references: references);
		//}

		//public static void DrawPropertyCustomValue(
		//	EffectProperty effectProperty,
		//	string labelPrefix,
		//	string displayName,
		//	bool allowReset,
		//	bool isKeywordProperty,
		//	AllIn13DShaderInspectorReferences references)
		//{
		//	MaterialProperty materialProperty = references.matProperties[effectProperty.propertyIndex];
		//	string label = $"{labelPrefix} {displayName}";
		//	string tooltip = materialProperty.name + "(C#)";

		//	EditorGUILayout.BeginHorizontal();

		//	DrawShaderProperty(materialProperty: materialProperty, label: label, tooltip: tooltip, isKeywordProperty: isKeywordProperty, references: references);
		//	if (allowReset)
		//	{
		//		DrawResetButton(materialProperty, references);
		//	}

		//	EditorGUILayout.EndHorizontal();
		//}

		public static void DrawProperty(
			MaterialProperty materialProperty, 
			string labelPrefix, 
			string displayName,
			string customValue,
			bool allowReset, 
			bool isKeywordProperty,
			EffectProperty.PropertyType propertyType,
			Plugin3DMaterialInspectorReferences references)
		{
			string label = $"{labelPrefix} {displayName}";
			string tooltip = PropertyNameCopyMenu.Tooltip(materialProperty.name);

			EditorGUILayout.BeginHorizontal();

			bool isShaderVariant = references.IsShaderVariant();

			if(isShaderVariant && isKeywordProperty)
			{
				DrawShaderPropertyCustomValue(label: label, tooltip: tooltip, propertyName: materialProperty.name, customValue: customValue, isKeywordProperty: isKeywordProperty, propertyType: propertyType, references: references);
			}
			else
			{
				DrawShaderProperty(materialProperty: materialProperty, label: label, tooltip: tooltip, isKeywordProperty: isKeywordProperty, references: references);
				if (allowReset)
				{
					DrawResetButton(materialProperty, references);
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		private static void DrawShaderProperty(
			MaterialProperty materialProperty, 
			string label, 
			string tooltip, 
			bool isKeywordProperty, 
			Plugin3DMaterialInspectorReferences references)
		{
			GUIContent propertyLabel = new GUIContent();
			propertyLabel.text = label;
			propertyLabel.tooltip = tooltip;

			EditorGUI.BeginChangeCheck();
			PropertyNameCopyMenu.ShaderProperty(references.editorMat, materialProperty, propertyLabel);
			if (EditorGUI.EndChangeCheck())
			{
				if (isKeywordProperty)
				{
					references.RefreshMaterialKeywords();
				}
			}
		}

		private static void DrawShaderPropertyCustomValue(
			string label,
			string tooltip,
			string propertyName,
			string customValue,
			bool isKeywordProperty,
			EffectProperty.PropertyType propertyType,
			Plugin3DMaterialInspectorReferences references)
		{
			EditorGUILayout.BeginHorizontal();
			GUIContent propertyLabel = new GUIContent();
			propertyLabel.text = label;
			propertyLabel.tooltip = tooltip;

			if(propertyType == EffectProperty.PropertyType.TOGGLE)
			{
				EditorGUILayout.Toggle(propertyLabel, true);
				PropertyNameCopyMenu.HandleLast(PropertyNameCopyMenu.PROPERTY, propertyName);
			}
			else
			{
				EditorGUILayout.LabelField(propertyLabel);
				PropertyNameCopyMenu.HandleLast(PropertyNameCopyMenu.PROPERTY, propertyName);
				EditorGUILayout.LabelField(customValue);
			}

			EditorGUILayout.EndHorizontal();
		}

		private static string[] GetKeywordNames(AllIn13DEffectConfig effectConfig)
		{
			string[] names = new string[effectConfig.keywords.Count];
			for (int i = 0; i < names.Length; i++) names[i] = effectConfig.keywords[i].keyword;
			return names;
		}

		public static void DrawResetButton(MaterialProperty targetProperty, Plugin3DMaterialInspectorReferences references)
		{
			GUIContent resetButtonLabel = new GUIContent();
			resetButtonLabel.text = "R";
			resetButtonLabel.tooltip = "Resets to default value";
			if (GUILayout.Button(resetButtonLabel, GUILayout.Width(20)))
			{
				for (int i = 0; i < references.targetMatInfos.Length; i++)
				{
					AbstractMaterialInfo matInfo = references.targetMatInfos[i];
					AllIn13DEffectConfig.ResetProperty(targetProperty, references, matInfo);
				}
			}
		}
	}
}