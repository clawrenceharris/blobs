using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	//A baked shader keeps the property block, library includes and inspector name of the version that baked it, but runs the
	//installed library, so a property an update adds reads 0 and a library that moved breaks it. Stale bakes get the current ones
	public class BakedShaderPropertiesSync : AssetPostprocessor
	{
		public class StaticValues
		{
			public bool isWriting;
		}

		private const string BAKED_SHADER_HEADER = "Shader \"AllIn13DShader - Baked Effects/";
		private const string BAKED_FILE_PREFIX = "AllIn13D_BakedEffects_";
		private const string PROPERTIES_START = "/*<COMMON_PROPERTIES_START>*/";
		private const string PROPERTIES_END = "/*<COMMON_PROPERTIES_END>*/";

		private const string LIBRARY_FILE_PREFIX = "AllIn13DShader";
		private const string LIBRARY_ANCHOR_FILE = "AllIn13DShaderCore.hlsl";
		private const string FEATURES_URP_FILE = "AllIn13DShader_FeaturesURP.hlsl";
		private const string FEATURES_URP_DEFINES_FILE = "AllIn13DShader_FeaturesURP_Defines.hlsl";
		private const string CUSTOM_EDITOR_CLASS = "AllIn1Assets.AllIn13DShader.AllIn13DShaderMaterialInspector";

		//The template block every baked shader of this project was last brought up to, so a launch only compares one file
		private const string SYNCED_TEMPLATE_PATH = "Library/AllIn1BakedShaderPropertiesSync.txt";

		private const string PENDING_KEY = "AllIn1_BakedShaderPropertiesSync_Pending";
		private const string SESSION_CHECKED_KEY = "AllIn1_BakedShaderPropertiesSync_SessionChecked";
		private const string IMPORTED_KEY = "AllIn1_BakedShaderPropertiesSync_Imported";

		private static readonly Regex PROPERTY_NAME = new Regex(@"^\s*(?:\[[^\]]*\]\s*)*(\w+)\s*\(", RegexOptions.Multiline);
		private static readonly Regex LINE_ENDING = new Regex(@"\r*\n|\r");
		private static readonly Regex BLOCK_COMMENT = new Regex(@"/\*.*?\*/", RegexOptions.Singleline);
		private static readonly Regex INCLUDE_PATH = new Regex(@"(#include(?:_with_pragmas)?[ \t]+"")([^""\r\n]*)""");
		private static readonly Regex OLD_CUSTOM_EDITOR = new Regex(@"(CustomEditor[ \t]+"")AllIn13DShader\.AllIn13DShaderMaterialInspector""");

		//Library files renamed since 2.74, by the name a bake of that version includes
		private static readonly Dictionary<string, string> RENAMED_LIBRARY_FILES = new Dictionary<string, string>
		{
			{ "AllIn13DShaderHelper_BIRP.hlsl", "ShaderHelper_BIRP.hlsl" },
			{ "AllIn13DShaderHelper_URP.hlsl", "ShaderHelper_URP.hlsl" },
			{ "AllIn13DShaderLightAddPass.hlsl", "AllIn13DShader_AddPass.hlsl" },
		};

		private static readonly StaticValues staticValues = new StaticValues();


		//Runs on every import, so it only compares paths: no file is read and the asset database is not queried here
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			if (staticValues.isWriting) return;

			bool pending = false;
			string importedBaked = null;
			for (int i = 0; i < importedAssets.Length; i++)
			{
				string path = importedAssets[i];
				if (path.EndsWith(Constants.FILE_NAME_SHADER_PROPERTIES, StringComparison.Ordinal))
				{
					pending = true;
				}
				else if (HasBakedShaderFileName(path))
				{
					importedBaked += path + "\n";
					pending = true;
				}
			}

			//A moved bake's library includes are relative to the folder it left
			for (int i = 0; i < movedAssets.Length; i++)
			{
				if (HasBakedShaderFileName(movedAssets[i]))
				{
					importedBaked += movedAssets[i] + "\n";
					pending = true;
				}
			}

			if (!pending) return;

			//Kept in the session, since a package update reloads the domain before the delayCall runs
			if (importedBaked != null)
			{
				SessionState.SetString(IMPORTED_KEY, SessionState.GetString(IMPORTED_KEY, string.Empty) + importedBaked);
			}

			SessionState.SetBool(PENDING_KEY, true);
			QueueSync();
		}

		//The name every bake writes, AllIn13D_BakedEffects_<profile>.shader
		private static bool HasBakedShaderFileName(string assetPath)
		{
			return assetPath.EndsWith(".shader", StringComparison.Ordinal)
				&& string.CompareOrdinal(assetPath, assetPath.LastIndexOf('/') + 1, BAKED_FILE_PREFIX, 0, BAKED_FILE_PREFIX.Length) == 0;
		}

		//Also once per session, since the update that brings this script imports the template before the script compiles
		[InitializeOnLoadMethod]
		private static void ResumeAfterDomainReload()
		{
			if (!SessionState.GetBool(SESSION_CHECKED_KEY, false))
			{
				SessionState.SetBool(SESSION_CHECKED_KEY, true);
				SessionState.SetBool(PENDING_KEY, true);
			}

			if (SessionState.GetBool(PENDING_KEY, false))
			{
				QueueSync();
			}
		}

		private static void QueueSync()
		{
			EditorApplication.delayCall -= SyncAll;
			EditorApplication.delayCall += SyncAll;
		}

		private static void SyncAll()
		{
			if (EditorApplication.isCompiling || EditorApplication.isUpdating)
			{
				QueueSync();
				return;
			}

			string[] importedBaked = SessionState.GetString(IMPORTED_KEY, string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			SessionState.EraseBool(PENDING_KEY);
			SessionState.EraseString(IMPORTED_KEY);

			string templatePath = FindTemplatePath();
			string templateBlock = templatePath != null ? ReadTemplateBlock(templatePath) : null;
			if (templateBlock == null) return;

			//An unchanged template means only newly imported baked shaders can be stale
			bool templateChanged = !File.Exists(SYNCED_TEMPLATE_PATH) || File.ReadAllText(SYNCED_TEMPLATE_PATH) != templateBlock;
			string[] candidates = templateChanged ? FindShaderPaths() : importedBaked;
			if (candidates.Length == 0 && !templateChanged) return;

			List<string> updated = new List<string>();
			HashSet<string> checkedPaths = new HashSet<string>();
			string libraryFolder = null;
			HashSet<string> libraryFileNames = null;
			bool failed = false;
			for (int i = 0; i < candidates.Length; i++)
			{
				string path = candidates[i];
				if (!checkedPaths.Add(path) || !IsBakedShader(path)) continue;

				if (libraryFileNames == null)
				{
					libraryFolder = FindLibraryFolder();
					libraryFileNames = libraryFolder != null ? GetLibraryFileNames(libraryFolder) : new HashSet<string>();
				}

				//Its includes cannot be checked without the library, so the next session tries again
				if (libraryFolder == null) failed = true;

				try
				{
					string text = File.ReadAllText(path);
					string upgraded = UpgradeBakedShader(text, EditorUtils.GetAssetParentFolder(path), libraryFolder, libraryFileNames, out bool needsRebake);
					if (needsRebake)
					{
						Debug.LogWarning($"[AllIn1] The baked shader '{path}' was made by a version of All In 1 3D Shader too old to update automatically, so its materials may not render as before. " +
							"Bake it again: on a material that uses it press Revert To Generic Shader, then Bake Shader Keywords.", AssetDatabase.LoadAssetAtPath<Shader>(path));
					}

					string synced = SyncPropertiesBlock(upgraded ?? text, templateBlock) ?? upgraded;
					if (synced == null) continue;

					File.WriteAllText(path, synced);
					updated.Add(path);
				}
				catch (Exception e)
				{
					failed = true;
					Debug.LogWarning($"[AllIn1] Could not update the baked shader '{path}'. Bake its material again. Details: {e.Message}");
				}
			}

			//After a failed write the template is not recorded, so the next session tries again
			if (templateChanged && !failed)
			{
				File.WriteAllText(SYNCED_TEMPLATE_PATH, templateBlock);
			}

			if (updated.Count == 0) return;

			staticValues.isWriting = true;
			try
			{
				for (int i = 0; i < updated.Count; i++)
				{
					AssetDatabase.ImportAsset(updated[i]);
				}
			}
			finally
			{
				staticValues.isWriting = false;
			}

			Debug.Log($"[AllIn1] Updated {updated.Count} baked shaders to this version of All In 1 3D Shader, so their materials render as before.");
		}

		//Null when the baked text has no marked property block, or already has every property of the template.
		//Only the block changes, written with the line ending it already had, so the rest of the file stays byte for byte
		public static string SyncPropertiesBlock(string bakedText, string templateBlock)
		{
			int start = bakedText.IndexOf(PROPERTIES_START, StringComparison.Ordinal);
			int end = start < 0 ? -1 : bakedText.IndexOf(PROPERTIES_END, start, StringComparison.Ordinal);
			if (end < 0) return null;
			end += PROPERTIES_END.Length;

			string block = bakedText.Substring(start, end - start);
			HashSet<string> present = GetPropertyNames(block);
			foreach (string name in GetPropertyNames(templateBlock))
			{
				if (!present.Contains(name))
				{
					Match lineEnding = LINE_ENDING.Match(block);
					string eol = lineEnding.Success ? lineEnding.Value : "\n";
					return bakedText.Substring(0, start) + EditorUtils.UnifyEOL(templateBlock).Replace("\n", eol) + bakedText.Substring(end);
				}
			}

			return null;
		}

		//Null when nothing changes. Only library include paths and the inspector name are rewritten, so the rest stays byte for byte
		public static string UpgradeBakedShader(string bakedText, string shaderFolder, string libraryFolder, HashSet<string> libraryFileNames, out bool needsRebake)
		{
			needsRebake = false;
			if (!bakedText.StartsWith(BAKED_SHADER_HEADER, StringComparison.Ordinal)) return null;

			string res = OLD_CUSTOM_EDITOR.Replace(bakedText, "${1}" + CUSTOM_EDITOR_CLASS + "\"");

			if (libraryFolder != null)
			{
				int featuresURP = 0;
				int featuresURPDefines = 0;
				bool unknownLibraryFile = false;

				res = INCLUDE_PATH.Replace(res, match =>
				{
					string includePath = match.Groups[2].Value.Replace('\\', '/');
					string fileName = includePath.Substring(includePath.LastIndexOf('/') + 1);

					string currentName;
					if (!RENAMED_LIBRARY_FILES.TryGetValue(fileName, out currentName)) currentName = fileName;

					if (!libraryFileNames.Contains(currentName))
					{
						unknownLibraryFile |= fileName.StartsWith(LIBRARY_FILE_PREFIX, StringComparison.Ordinal);
						return match.Value;
					}

					if (currentName == FEATURES_URP_FILE) featuresURP++;
					if (currentName == FEATURES_URP_DEFINES_FILE) featuresURPDefines++;

					string target = libraryFolder + "/" + currentName;
					if (string.Equals(ResolveIncludePath(shaderFolder, includePath), target, StringComparison.OrdinalIgnoreCase)) return match.Value;

					return match.Groups[1].Value + GetRelativeAssetPath(shaderFolder, target) + "\"";
				});

				//Bakes from before 2.21 lack the URP defines include and the Built-in pass pragmas, which only a new bake writes
				needsRebake = unknownLibraryFile || featuresURP > featuresURPDefines;
			}

			return string.Equals(res, bakedText, StringComparison.Ordinal) ? null : res;
		}

		//The path Path.GetRelativePath writes into a bake, without resolving against the working folder
		public static string GetRelativeAssetPath(string fromFolder, string toPath)
		{
			string[] from = fromFolder.Split('/');
			string[] to = toPath.Split('/');

			int common = 0;
			while (common < from.Length && common < to.Length - 1 && string.Equals(from[common], to[common], StringComparison.OrdinalIgnoreCase))
			{
				common++;
			}

			string res = string.Empty;
			for (int i = common; i < from.Length; i++)
			{
				res += "../";
			}

			return res + string.Join("/", to, common, to.Length - common);
		}

		//Null when the path climbs above the project
		private static string ResolveIncludePath(string shaderFolder, string includePath)
		{
			bool projectRelative = includePath.StartsWith("Assets/", StringComparison.Ordinal) || includePath.StartsWith("Packages/", StringComparison.Ordinal);
			string combined = projectRelative ? includePath : shaderFolder + "/" + includePath;

			List<string> segments = new List<string>();
			foreach (string segment in combined.Split('/'))
			{
				if (segment.Length == 0 || segment == ".") continue;

				if (segment != "..")
				{
					segments.Add(segment);
				}
				else if (segments.Count > 0)
				{
					segments.RemoveAt(segments.Count - 1);
				}
				else
				{
					return null;
				}
			}

			return string.Join("/", segments);
		}

		//The marked block as a bake writes it
		public static string ReadTemplateBlock(string templatePath)
		{
			string text = EditorUtils.ReadFileTextWithTabs(templatePath, 2);

			int start = text.IndexOf(PROPERTIES_START, StringComparison.Ordinal);
			int end = start < 0 ? -1 : text.IndexOf(PROPERTIES_END, start, StringComparison.Ordinal);
			if (end < 0) return null;

			return text.Substring(start, end + PROPERTIES_END.Length - start);
		}

		public static HashSet<string> GetPropertyNames(string propertiesText)
		{
			HashSet<string> res = new HashSet<string>();
			string uncommented = BLOCK_COMMENT.Replace(EditorUtils.UnifyEOL(propertiesText), string.Empty);
			foreach (Match match in PROPERTY_NAME.Matches(uncommented))
			{
				res.Add(match.Groups[1].Value);
			}

			return res;
		}

		public static bool IsBakedShader(string assetPath)
		{
			if (!assetPath.EndsWith(".shader", StringComparison.Ordinal) || !File.Exists(assetPath)) return false;

			try
			{
				using (StreamReader reader = new StreamReader(assetPath))
				{
					string firstLine = reader.ReadLine();
					return firstLine != null && firstLine.StartsWith(BAKED_SHADER_HEADER, StringComparison.Ordinal);
				}
			}
			catch (IOException)
			{
				return false;
			}
		}

		//The library a bake made now includes. A pre 2.8 plugin folder left in the project lacks the renamed files
		public static string FindLibraryFolder()
		{
			string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(LIBRARY_ANCHOR_FILE));
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				if (!path.EndsWith("/" + LIBRARY_ANCHOR_FILE, StringComparison.Ordinal)) continue;

				string folder = EditorUtils.GetAssetParentFolder(path);
				bool hasRenamedFiles = true;
				foreach (string currentName in RENAMED_LIBRARY_FILES.Values)
				{
					hasRenamedFiles &= File.Exists(folder + "/" + currentName);
				}

				if (hasRenamedFiles) return folder;
			}

			return null;
		}

		public static HashSet<string> GetLibraryFileNames(string libraryFolder)
		{
			HashSet<string> res = new HashSet<string>();
			foreach (string filePath in Directory.GetFiles(libraryFolder, "*.hlsl"))
			{
				res.Add(Path.GetFileName(filePath));
			}

			return res;
		}

		private static string[] FindShaderPaths()
		{
			string[] guids = AssetDatabase.FindAssets("t:Shader", new[] { "Assets" });
			string[] res = new string[guids.Length];
			for (int i = 0; i < guids.Length; i++)
			{
				res[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
			}

			return res;
		}

		private static string FindTemplatePath()
		{
			string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(Constants.FILE_NAME_SHADER_PROPERTIES));
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				if (path.EndsWith(Constants.FILE_NAME_SHADER_PROPERTIES, StringComparison.Ordinal))
				{
					return path;
				}
			}

			return null;
		}
	}
}
