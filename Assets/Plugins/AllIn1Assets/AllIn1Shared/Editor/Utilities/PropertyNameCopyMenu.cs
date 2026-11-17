using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets
{
	// Right click a material inspector label to copy its shader property or keyword name; each drop in reserves its rect first so the field never sees the click
	public static class PropertyNameCopyMenu
	{
		public const string PROPERTY = "property name";
		public const string KEYWORD = "keyword";

		private static readonly Dictionary<string[], GUIContent[]> popupContentsCache = new Dictionary<string[], GUIContent[]>();
		private static readonly Dictionary<string, string> tooltipCache = new Dictionary<string, string>();

		// Cached per name so inspectors no longer build a tooltip string per row on every repaint
		public static string Tooltip(string name)
		{
			if (name == null) return null;
			if (!tooltipCache.TryGetValue(name, out string tooltip))
			{
				tooltip = name + " (C#), right click to copy";
				tooltipCache[name] = tooltip;
			}
			return tooltip;
		}

		// True on a right click inside rect; the right mouse down before it is swallowed so toggles and fields under the label never react
		public static bool IsMenuClick(Rect rect)
		{
			Event evt = Event.current;
			bool isRightMouseDown = evt.type == EventType.MouseDown && evt.button == 1;
			if (!isRightMouseDown && evt.type != EventType.ContextClick) return false;
			if (!rect.Contains(evt.mousePosition)) return false;

			if (isRightMouseDown)
			{
				evt.Use();
				return false;
			}
			return true;
		}

		// One "Copy" item per name, the checked one ticked; call only after IsMenuClick returned true
		public static void ShowMenu(string what, string[] names, int checkedIndex = -1)
		{
			GenericMenu menu = new GenericMenu();
			for (int i = 0; i < names.Length; i++)
			{
				string name = names[i];
				if (string.IsNullOrEmpty(name)) continue;
				// Name in parentheses: a trailing " _Name" token could be read as a menu shortcut
				menu.AddItem(new GUIContent($"Copy {what} ({name})"), i == checkedIndex, () => EditorGUIUtility.systemCopyBuffer = name);
			}

			if (menu.GetItemCount() > 0) menu.ShowAsContext();
			Event.current.Use();
		}

		public static void Handle(Rect rect, string what, string name)
		{
			if (string.IsNullOrEmpty(name)) return;
			if (IsMenuClick(rect)) ShowMenu(what, new[] { name });
		}

		// For read only rows that never react to a right click: checks the rect of the control just drawn
		public static void HandleLast(string what, string name)
		{
			EventType type = Event.current.type;
			if (type != EventType.MouseDown && type != EventType.ContextClick) return;
			Handle(GUILayoutUtility.GetLastRect(), what, name);
		}

		public static Rect LabelRect(Rect row)
		{
			return new Rect(row.x, row.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
		}

		// Drop in for MaterialEditor.ShaderProperty(prop, label): the same reserve then draw pair Unity runs internally
		public static void ShaderProperty(MaterialEditor materialEditor, MaterialProperty prop, GUIContent label)
		{
			float height = materialEditor.GetPropertyHeight(prop, label.text);
			Rect row = EditorGUILayout.GetControlRect(true, height, EditorStyles.layerMaskField);
			Handle(LabelRect(row), PROPERTY, prop.name);
			materialEditor.ShaderProperty(row, prop, label);
		}

		public static void ShaderProperty(MaterialEditor materialEditor, MaterialProperty prop)
		{
			ShaderProperty(materialEditor, prop, new GUIContent(prop.displayName, Tooltip(prop.name)));
		}

		// Drop in for GUILayout.Toggle(value, label)
		public static bool Toggle(bool value, GUIContent label, string what, string name)
		{
			return Toggle(value, label, GUI.skin.toggle, what, name);
		}

		// Drop in for GUILayout.Toggle(value, label, style)
		public static bool Toggle(bool value, GUIContent label, GUIStyle style, string what, string name)
		{
			Rect rect = GUILayoutUtility.GetRect(label, style);
			Handle(rect, what, name);
			return GUI.Toggle(rect, value, label, style);
		}

		// Drop in for EditorGUILayout.Toggle(label, value): the rect Unity's internal GetToggleRect reserves, label plus a 14 px box and 5 px spacing
		public static bool ToggleField(GUIContent label, bool value, string what, string name)
		{
			float width = EditorGUIUtility.labelWidth + 19f;
			float height = EditorGUIUtility.singleLineHeight;
			Rect rect = GUILayoutUtility.GetRect(width, width, height, height, EditorStyles.numberField);
			Handle(rect, what, name);
			return EditorGUI.Toggle(rect, label, value);
		}

		// Drop in for EditorGUILayout.BeginToggleGroup(label, toggle): same header, disabled scope and vertical group, so Unity's EndToggleGroup still closes it
		public static bool BeginToggleGroup(GUIContent label, bool toggle, string keyword)
		{
			Rect rect = EditorGUILayout.GetControlRect(true);
			Handle(rect, KEYWORD, keyword);
			toggle = EditorGUI.ToggleLeft(rect, label, toggle, EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(!toggle);
			GUILayout.BeginVertical();
			return toggle;
		}

		// Row for a drop in of EditorGUILayout.Popup(label, index, options): same GetControlRect call Unity makes
		public static Rect PopupRow()
		{
			return EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, EditorStyles.popup);
		}

		// Popup options as GUIContent, cached per array, for the public EditorGUI.Popup overload that keeps the label tooltip
		public static GUIContent[] PopupContents(string[] options)
		{
			if (popupContentsCache.TryGetValue(options, out GUIContent[] contents) && ContentsMatch(contents, options)) return contents;

			contents = new GUIContent[options.Length];
			for (int i = 0; i < options.Length; i++) contents[i] = new GUIContent(options[i]);
			popupContentsCache[options] = contents;
			return contents;
		}

		private static bool ContentsMatch(GUIContent[] contents, string[] options)
		{
			if (contents.Length != options.Length) return false;
			for (int i = 0; i < options.Length; i++)
			{
				if (contents[i].text != options[i]) return false;
			}
			return true;
		}
	}
}
