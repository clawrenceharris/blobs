using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.Rendering;


namespace AllIn1Assets
{
	public class RenderPipelineChecker
	{
		public class Values
		{
			public bool isHDRP;
			public bool isURP;
			public bool isStandardRP;
			public RenderPipelineEnum currentRenderPipeline;
		}

		private const string SYMBOL_URP		= "ALLIN1_URP";
		private const string SYMBOL_HDRP	= "ALLIN1_HDRP";
		private const string SYMBOL_BIRP	= "ALLIN1_BIRP";

		private const string HDRP_PACKAGE = "HDRenderPipelineAsset"; 
		private const string URP_PACKAGE = "UniversalRenderPipelineAsset";

		private static readonly Values values = new Values();

		public static bool IsHDRP()
		{
			return values.isHDRP;
		}

		public static bool IsURP()
		{
			return values.isURP;
		}

		public static bool IsStandardRP()
		{
			return values.isStandardRP;
		}

		public static RenderPipelineEnum GetCurrentRenderPipeline()
		{
			return values.currentRenderPipeline;
		}

		public static void RefreshData()
		{
			values.isHDRP = DoesTypeExist(HDRP_PACKAGE);
			values.isURP = DoesTypeExist(URP_PACKAGE);
			values.isStandardRP = !(values.isHDRP || values.isURP);

			if (values.isURP)
			{
				values.currentRenderPipeline = RenderPipelineEnum.URP;
			}
			else if (values.isHDRP)
			{
				values.currentRenderPipeline = RenderPipelineEnum.HDRP;
			}
			else
			{
				values.currentRenderPipeline = RenderPipelineEnum.BIRP;
			}
		}

		public static bool DoesTypeExist(string className)
		{
			return TypeCache.GetTypesDerivedFrom<RenderPipelineAsset>()
				.Any(type => type.Name == className);
		}

		public static bool IsRenderPipelineDefined()
		{
			bool res = false;

			BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
			BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

			NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
			string[] defineSymbols = new string[0];
			PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out defineSymbols);

			for (int i = defineSymbols.Length - 1; i >= 0; i--)
			{
				if (defineSymbols[i] == SYMBOL_URP || defineSymbols[i] == SYMBOL_HDRP || defineSymbols[i] == SYMBOL_BIRP)
				{
					res = true;
				}
			}

			return res;
		}

		private static string[] GetSymbolsWithoutPipeline()
		{
			BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
			BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

			NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
			string[] defineSymbols = new string[0];
			PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out defineSymbols);

			for (int i = defineSymbols.Length - 1; i >= 0; i--)
			{
				if (defineSymbols[i] == SYMBOL_URP || defineSymbols[i] == SYMBOL_HDRP || defineSymbols[i] == SYMBOL_BIRP)
				{
					ArrayUtility.RemoveAt(ref defineSymbols, i);
				}
			}

			return defineSymbols;
		}

		public static void RemovePipelineSymbols()
		{
			string[] defineSymbols = GetSymbolsWithoutPipeline();
			ApplyDefineSymbols(defineSymbols);
		}

		private static void ApplyDefineSymbols(string[] defineSymbols)
		{
			BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

			PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defineSymbols);
		}

		public static void CheckRenderPipeline()
		{
			RefreshData();

			RenderPipelineEnum lastRenderPipeline = (RenderPipelineEnum)SessionState.GetInt(GlobalConstants.LAST_RENDER_PIPELINE_CHECKED_KEY, 0);

			if (lastRenderPipeline != values.currentRenderPipeline || !IsRenderPipelineDefined())
			{
				string[] defineSymbols = GetSymbolsWithoutPipeline();
				if (values.isURP)
				{
					ArrayUtility.Add(ref defineSymbols, SYMBOL_URP); 
				}
				else if (values.isHDRP)
				{
					ArrayUtility.Add(ref defineSymbols, SYMBOL_HDRP);
				}
				else
				{
					ArrayUtility.Add(ref defineSymbols, SYMBOL_BIRP);
				}

				ApplyDefineSymbols(defineSymbols);
				SessionState.SetInt(GlobalConstants.LAST_RENDER_PIPELINE_CHECKED_KEY, (int)values.currentRenderPipeline);
			}
		}

		[InitializeOnLoadMethod]
		public static void ScriptsReloaded()
		{
			if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Secondary)
				return;

			CheckRenderPipeline();
		}
	}
}
