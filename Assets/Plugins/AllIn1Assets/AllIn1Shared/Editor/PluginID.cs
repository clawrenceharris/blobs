using UnityEngine;

namespace AllIn1Assets
{
	public enum PluginID
	{
		UNDEFINED = -1,
		[InspectorName("AllIn1 3D Shader")]		ALL_IN_1_3D_SHADER = 0,
		[InspectorName("AllIn1 Text Shader")]	ALLIN1_TEXT_SHADER = 2,
		[InspectorName("AllIn1 Shader Nodes")]	ALLIN1_SHADER_NODES = 3,
		[InspectorName("AllIn1 Springs")]		ALLIN1_SPRINGS = 4,
		[InspectorName("AllIn1 VFX Toolkit")]	ALLIN1_VFX = 5,
		[InspectorName("AllIn1 Sprite Shader")] ALLIN1_SPRITE = 6,
	}
}