using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering;


#if ALLIN1_URP

using UnityEditor.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.Universal;
#else
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;
#endif

#endif

namespace AllIn1Assets
{
	//Answers one question after every editor load: is this a URP project where some plugin with shaders has
	//no stored answer about its render pipeline configuration yet? The answer derives from what the
	//configs store, never from noticing an import, so a load that cannot answer (define not compiled
	//yet, no pipeline asset assigned, package still importing) simply asks the same question next time
	public static class URPConfigurator
	{
		private const string PIPELINE_PACKAGE_PATH = "Packages/com.unity.render-pipelines";

		public static bool IsURPCorrectlyConfigured()
		{
			bool res = true;
			if (GraphicsSettings.currentRenderPipeline == null)
			{
				res = false;
			}
			 
			return res;
		}

		//A slot with no stored config counts as unanswered: that is what makes a reinstall, a reset
		//and a plugin added later ask again, with no flag anyone has to remember to set
		public static bool AnyPluginAwaitingAnswer(List<AbstractPluginConfig> storedConfigs)
		{
			for (int i = 0; i < storedConfigs.Count; i++)
			{
				if (storedConfigs[i] == null || !storedConfigs[i].IsURPConfiguredFirstTime()) return true;
			}

			return false;
		}

#if ALLIN1_URP

		private const string KEY_PIPELINE_PACKAGE_REMOVED = "ALLIN13DSHADER_PIPELINE_PACKAGE_REMOVED_KEY";

		//Scheduled past the load callback by the Initializer, and once more after a reset. Read only
		//until it has something to ask: the configs it peeks are loaded, never created, so a reload
		//in a configured project costs one asset load per plugin and leaves nothing behind
		public static void Reconcile()
		{
			if (Application.isBatchMode || BuildPipeline.isBuildingPlayer) return;

			if (RenderPipelineChecker.GetCurrentRenderPipeline() != RenderPipelineEnum.URP) return;

			//Nothing can be configured without a pipeline asset. The next load asks again once there is one
			if (GraphicsSettings.currentRenderPipeline == null) return;

			if (!AnyPluginAwaitingAnswer(PluginRegistry.PeekAllNeedingURPConfiguration())) return;

			Configure();
		}

		public static void Configure(bool forceConfigure = false)
		{
			RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;

			if (pipeline == null)
			{
				return;
			}

			//Only shaderless plugins installed, like the Springs Toolkit alone: nothing to configure, Depth Priming included
			List<AbstractPluginConfig> plugins = PluginRegistry.ResolveAllNeedingURPConfiguration();
			if (plugins.Count == 0)
			{
				return;
			}

			if (forceConfigure)
			{
				ConfigureURPAccepted(pipeline, plugins);

				return;
			}

			bool configureAccepted = EditorUtility.DisplayDialog("URP automatic configuration", "Do you want to automatically configure this URP project to work with the installed AllIn1 assets?\n" +
																							"If you Cancel, the manual setup is in the documentation:\n" +
																							GetDocURLs(plugins), "Configure", "Cancel");
			if (configureAccepted)
			{
				ConfigureURPAccepted(pipeline, plugins);
			}
			else
			{
				ConfigureURPDenied(pipeline);
			}
		}

		//The plugins still waiting for an answer raised the prompt, so theirs are the pages linked. A forced run links every plugin
		private static string GetDocURLs(List<AbstractPluginConfig> plugins)
		{
			List<string> urls = new List<string>();
			bool anyAwaitingAnswer = AnyPluginAwaitingAnswer(plugins);

			for (int i = 0; i < plugins.Count; i++)
			{
				if (anyAwaitingAnswer && plugins[i].IsURPConfiguredFirstTime()) continue;

				string url = plugins[i].GetRenderPipelineDocURL();
				if (!urls.Contains(url))
				{
					urls.Add(url);
				}
			}

			return string.Join("\n", urls);
		}

		//The answer is recorded even when the renderer data cannot be found, or the question would
		//come back on every load. The plugins only get configured when there is something to configure
		private static void ConfigureURPAccepted(RenderPipelineAsset pipeline, List<AbstractPluginConfig> plugins)
		{
			UniversalRendererData universalRendererData = GetUniversalRendererDataByPipelineAsset(pipeline);

			if (universalRendererData != null)
			{
				Undo.RecordObject(universalRendererData, "");

				universalRendererData.depthPrimingMode = DepthPrimingMode.Disabled;

				EditorUtility.SetDirty(universalRendererData);
				EditorUtility.SetDirty(pipeline);
			}
			else
			{
				Debug.LogWarning("[AllIn1] The active URP asset does not use a Universal Renderer (it may use the 2D Renderer), so nothing was configured automatically. " +
					"Your answer is saved. If you switch to a Universal Renderer later, follow the render pipeline setup in the documentation:\n" +
					GetDocURLs(plugins));
			}

			PluginRegistry.ConfigureURPAccepted(universalRendererData, pipeline);
		}

		private static void ConfigureURPDenied(RenderPipelineAsset pipeline)
		{
			PluginRegistry.ConfigureURPDenied(pipeline);
		}

		private static UniversalRendererData GetUniversalRendererDataByPipelineAsset(RenderPipelineAsset pipeline)
		{
			UniversalRenderPipelineAsset universalPipeline = pipeline as UniversalRenderPipelineAsset;
			if (universalPipeline == null)
			{
				return null;
			}

			ScriptableRenderer scriptableRenderer = universalPipeline.GetRenderer(0);

			FieldInfo rendererDataListFieldInfo = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			ScriptableRendererData[] scriptableRendererDatas = rendererDataListFieldInfo.GetValue(pipeline) as ScriptableRendererData[];
			if (scriptableRendererDatas == null || scriptableRendererDatas.Length == 0)
			{
				return null;
			}

			UniversalRendererData res = scriptableRendererDatas[0] as UniversalRendererData;

			return res;
		}

		public static void CheckURPRemoved(string[] deletedAssets, bool didDomainReload)
		{
			bool removeUniversalForward = false;
			for (int i = 0; i < deletedAssets.Length; i++)
			{
				if (deletedAssets[i].StartsWith(PIPELINE_PACKAGE_PATH, System.StringComparison.Ordinal))
				{
					removeUniversalForward = true;
					break;
				}
			}

			SessionState.SetBool(KEY_PIPELINE_PACKAGE_REMOVED, removeUniversalForward);


			if (!didDomainReload && SessionState.GetBool(KEY_PIPELINE_PACKAGE_REMOVED, false))
			{
				RenderPipelineChecker.RemovePipelineSymbols();
				SessionState.SetBool(KEY_PIPELINE_PACKAGE_REMOVED, false);

				Debug.LogWarning("Render pipeline package removed. Configuring AllIn13DShader...");
			}
		}

		public static bool IsRenderPipelineAssigned()
		{
			bool res = GraphicsSettings.currentRenderPipeline != null;
			return res;
		}

		public static bool IsURPMissconfigured()
		{
			bool res = false;

			bool renderPipelineAssigned = GraphicsSettings.currentRenderPipeline != null;

			return res;
		}
#endif
	}
}
