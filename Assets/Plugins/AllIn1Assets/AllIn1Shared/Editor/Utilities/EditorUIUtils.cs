using UnityEditor;
using UnityEngine;

namespace AllIn1Assets
{
	public static class EditorUIUtils
	{
		public static void AddSpaces(int numSpaces)
		{
			for(int i = 0; i < numSpaces; i++)
			{
				EditorGUILayout.Space();
			}
		}

		public static Texture DrawTextureField(Texture tex, float height, string label, ref Rect lastRect)
		{
			Rect rect = new Rect(lastRect);
			rect.height = height;

			GUIStyle labelStyle = EditorStyles.label;
			labelStyle.alignment = TextAnchor.UpperLeft;

			Rect labelRect = new Rect(rect);
			labelRect.width = 200f;
			EditorGUI.LabelField(labelRect, new GUIContent(label), labelStyle);

			GUIStyle textureStyle = EditorStyles.objectField;
			textureStyle.alignment = TextAnchor.MiddleLeft;

			Rect textureRect = new Rect(rect);
			textureRect.width = height;
			textureRect.height = height;
			textureRect.x = rect.width - textureRect.width - 10f;

			Texture res = (Texture)EditorGUI.ObjectField(textureRect, tex, typeof(Texture), false);
			lastRect.y += height;

			return res;
		}

		public static void DrawWarningLabel(string label, float height, ref Rect lastRect)
		{
			GUIStyle labelStyle = EditorStyles.helpBox;
			Color lastColor = labelStyle.normal.textColor;
			labelStyle.normal.textColor = Color.yellow;

			Rect rect = new Rect(lastRect);
			rect.height = height;

			EditorGUI.LabelField(rect, label, labelStyle);

			labelStyle.normal.textColor = lastColor;

			lastRect.y += height;
		}

		public static int DrawIntRange(int value, int minValue, int maxValue, string label, float height, ref Rect lastRect)
		{
			int res = value;

			Rect rect = new Rect(lastRect);
			rect.height = height;

			res = EditorGUI.IntSlider(rect, label, value, minValue, maxValue);

			lastRect.y += height;

			return res;
		}

		public static float DrawFloatRange(float value, float minValue, float maxValue, string label, float height, ref Rect lastRect)
		{
			float res = value;

			Rect rect = new Rect(lastRect);
			rect.height = height;

			res = EditorGUI.Slider(rect, label, value, minValue, maxValue);

			lastRect.y += height;

			return res;
		}

		public static bool DrawButton(string label, float height, ref Rect lastRect)
		{
			Rect rect = new Rect(lastRect);
			rect.height = height;

			bool res = GUI.Button(rect, label);

			lastRect.y += height;

			return res;
		}
	}
}