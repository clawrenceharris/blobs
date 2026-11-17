using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.SharedExtensions
{
	public class ShadersCreatorTool
	{
		private const string STENCIL_BLOCK_TEMPLATE_NAME = "StencilBlock";
		private const string STENCIL_BLOCK_TAG = "/*<STENCIL_BLOCK>*/";
		private const string STENCIL_REF_ENTRY = "[_StencilRef]";

		private static readonly string[] TEMPLATE_TAGS = new string[]
		{
			"COMMON_PROPERTIES",
			"BASE_PASS",
			"FORWARD_ADD_PASS",
			"SHADOW_CASTER_PASS",
			"CUSTOM_EDITOR",
			"BASE_PASS_URP",
			"SHADOW_CASTER_PASS_URP",
			"DEPTH_ONLY_PASS_URP", 
			"DEPTH_NORMALS_PASS_URP",
			"META_PASS_URP",
		};

		private const string REGEX_CORE = @"\/\*<{0}_START>\*\/\s*([\t\r\ ]*(?:.*\n)*)[\t\r\ ]*\s+\/\*<{0}_END>\*\/";
		private const string TAG_FORMAT = "<{0}>";

		public virtual void BuildShaderFiles(Plugin3DConfig pluginConfig)
		{
			string mainShaderPath = AssetDatabase.GetAssetPath(pluginConfig.shStandard);

			string shaderFileText = File.ReadAllText(mainShaderPath);

			string stencilBlockPath = EditorUtils.CombineAssetPath(pluginConfig.GlobalTemplatesFolderPath, STENCIL_BLOCK_TEMPLATE_NAME) + Constants.EXTENSION_ALLIN1TEMPLATE;
			string stencilBlockContent = File.ReadAllText(stencilBlockPath);

			string[] shaderTemplates = pluginConfig.GetShaderTemplates();
			for (int i = 0; i < shaderTemplates.Length; i++)
			{
				string templatePath = EditorUtils.CombineAssetPath(pluginConfig.GlobalTemplatesFolderPath, shaderTemplates[i]) + Constants.EXTENSION_ALLIN1TEMPLATE;
				
				string templateText = File.ReadAllText(templatePath);
				string newShaderFileText = SearchAndReplaceTemplateTags(shaderFileText, templateText);

				//The tag arrives with the base pass data, but only templates whose own passes test the stencil (the outline ones) need it written
				if (templateText.Contains(STENCIL_REF_ENTRY))
				{
					newShaderFileText = newShaderFileText.Replace(STENCIL_BLOCK_TAG, stencilBlockContent);
				}

				string newShaderFileName = shaderTemplates[i].Replace("_Template", "");
				string newShaderPath = EditorUtils.CombineAssetPath(pluginConfig.GenericShadersFolderPath, newShaderFileName + ".shader");

				//Skip the write and import when the generated shader hasn't changed
				if (File.Exists(newShaderPath))
				{
					string existingShaderFileText = File.ReadAllText(newShaderPath);
					if (string.Equals(existingShaderFileText, newShaderFileText, System.StringComparison.Ordinal))
					{
						continue;
					}
				}

				File.WriteAllText(newShaderPath, newShaderFileText);
				AssetDatabase.ImportAsset(newShaderPath, ImportAssetOptions.ForceUpdate);
			}
		}

		private string SearchAndReplaceTemplateTags(string shaderFileText, string templateText)
		{
			string res = templateText;

			for (int i = 0; i < TEMPLATE_TAGS.Length; i++)
			{
				string tagData = GetDataByTag(shaderFileText, TEMPLATE_TAGS[i]);
				res = OverrideTagWithData(TEMPLATE_TAGS[i], tagData, res);
			}

			return res;
		}

		private string GetDataByTag(string shaderFileTex, string tag)
		{
			string res = string.Empty;

			string regex = string.Format(REGEX_CORE, tag);
			MatchCollection matchCollection = Regex.Matches(shaderFileTex, regex);
			if(matchCollection.Count > 0)
			{
				res = matchCollection[0].Groups[1].Value.TrimStart().TrimEnd();
			}

			return res;
		}

		private string OverrideTagWithData(string tag, string data, string targetFileTxt)
		{
			string res = targetFileTxt;

			string formattedTag = string.Format(TAG_FORMAT, tag);
			res = res.Replace(formattedTag, data);

			return res;
		}
	}
}