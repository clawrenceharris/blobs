namespace AllIn1Assets.SharedExtensions
{
	public static class Constants
	{
		public const string EFFECT_ATTRIBUTE_PREFIX = "Effect(";
		public const string EFFECT_PROPERTY_ATTRIBUTE_PREFIX = "EffectProperty(";
		public const string HEADER_PREFIX = "Header(";
		public const string SINGLE_PROPERTY_ATTRIBUTE = "SingleProperty";
		public const string ADVANCED_PROPERTY_ATTRIBUTE = "AdvancedProperty";

		public const string KEYWORD_NONE = "NONE";

		//Special Properties
		public const string MATPROPERTY_RENDERING_MODE = "_RenderPreset";
		public const string MATPROPERTY_BLEND_SRC = "_BlendSrc";
		public const string MATPROPERTY_BLEND_DST = "_BlendDst";
		public const string KEYWORD_ALLIN13D_SURFACE_TRANSPARENT = "_ALLIN13D_SURFACE_TRANSPARENT";

		//Strings
		public const string HDR_STR = "HDR";
		public const string KEYWORD_ENUM_STR = "KeywordEnum";
		public const string ON_STR = "On";
		public const string OFF_STR = "Off";
		public const string DISABLED_ENUM_OPTION_STR = "None";

		//Regex
		public const string REGEX_EFFECT = @"\(EffectID#([\w\s]+),.*GroupID#([\w\s]+)(?:,.*AllowReset#([\w\s]+))?(?:,.*DependentOn#([\w\s]+))?(?:,.*IncompatibleWith#([\w\s]+))?(?:,.*Doc#([\w\s\\\.]+))?(?:,.*CustomDrawer#([\w\s]+))?(?:,.*ExtraPasses#[\s]*\(([\w\s,]+)\))?\)";
		public const string REGEX_EFFECT_PROPERTY = @"EffectProperty\((.*)\)";
		//public const string REGEX_EFFECT_PROOPERTY_COMPLETE = @"EffectProperty\(ParentEffect# ([\w\s]+), Keywords\((.*)\)\)";
		public const string REGEX_EFFECT_PROOPERTY_COMPLETE = @"EffectProperty\(ParentEffect# ([\w\s]+)(?:,.*KeywordsOp# ([\w]+))?(?:,.*IncompatibleWithKws\(([\w]+)\))?, Keywords\((.*)\), AllowReset# ([\w]+)\)";
		public const string REGEX_PARENT_EFFECT_KEYWORDS = @".*\((.*)\)";
		public const string REGEX_KEYWORDS_ENUM = @"KeywordEnum\((.*)\)";
		public const string REGEX_TOGGLE = @"Toggle\((.*)\)";

		//Drawers IDs
		public const string GENERAL_EFFECT_DRAWER_ID = "GENERAL_EFFECT_DRAWER";
		public const string TRIPLANAR_EFFECT_DRAWER_ID = "TRIPLANAR_EFFECT_DRAWER";
		public const string COLOR_RAMP_EFFECT_DRAWER_ID = "COLOR_RAMP_EFFECT_DRAWER";
		public const string OUTLINE_DRAWER_ID = "OUTLINE_DRAWER_ID";
		public const string TEXTURE_BLENDING_EFFECT_DRAWER_ID = "TEXTURE_BLENDING_EFFECT_DRAWER";
		public const string NORMAL_MAP_EFFECT_DRAWER_ID = "NORMAL_MAP_EFFECT_DRAWER";

		//
		public const string INCLUDE_LINE_FORMAT = @"#include ""{0}""";
		public const string DEFINE_LINE_FORMAT = @"#define {0}";

		//Assembly Name
		public const string PLUGINS_3D_COMMON_ASSEMBLY_NAME = "AllIn1SharedExtensionsAssembly";

		//Folder Names
		public const string FOLDER_NAME_SHARED_EXTENSIONS	= "AllIn1SharedExtensions";
		public const string FOLDER_NAME_EFFECTS_PROFILES	= "Effects Profiles";
		public const string FOLDER_NAME_TEMPLATES			= "Templates";
		public const string FOLDER_NAME_SHADERS				= "Shaders";
		public const string FOLDER_NAME_SHADER_LIBRARY		= "ShaderLibrary";
		public const string FOLDER_NAME_URP_SETTINGS		= "URP Settings";
		public const string FOLDER_NAME_SHADER_PASSES		= "Shader Passes";
		public const string FOLDER_NAME_BAKED_SHADERS		= "Baked Shaders";

		//Asset Names
		public const string ASSET_NAME_URP_SETTINGS = "URPSettings";
		public const string ASSET_NAME_URP_SETTINGS_USER_PREF = "URPSettingsUserPref";

		//File Names
		public const string FILE_NAME_SHADER_FEATURES_TEMPLATE	= "ShaderFeaturesTemplate.allin1template";
		public const string FILE_NAME_SHADER_TEMPLATE			= "ShaderTemplate.allin1template";
		public const string FILE_NAME_PASS_TEMPLATE_BIRP		= "PassTemplate_BIRP.allin1template";
		public const string FILE_NAME_PASS_TEMPLATE_URP			= "PassTemplate_URP.allin1template";
		//Casing matters on case sensitive filesystems: the files in Editor/Templates are .allIn1Template,
		//the ones in Editor/Effects Profiles/Templates are .allin1template
		public const string FILE_NAME_HLSL_LIBRARY_TEMPLATE		= "HLSLLibrary_Template.allIn1Template";
		public const string FILE_NAME_FEATURES_URP_DEFINES		= "FeaturesURP_Defines.hlsl";
		public const string FILE_NAME_SHADER_PROPERTIES			= "AllIn13DShader_ShaderProperties.allin1data";
		public const string FILE_NAME_SHADER_PASS_COLLECTION	= "ShaderPassCollection.asset";
		public const string FILE_NAME_BIRP_HELPER				= "ShaderHelper_BIRP.hlsl";
		public const string FILE_NAME_URP_HELPER				= "ShaderHelper_URP.hlsl";

		//Extensions
		public const string EXTENSION_HLSL = ".hlsl";
		//Only appended to the bare template names under Editor/Templates
		public const string EXTENSION_ALLIN1TEMPLATE = ".allIn1Template";
		public const string EXTENSION_ASSET = ".asset";

		//Suffix
		public const string SUFFIX_SHADER_FEATURES = "_ShaderFeatures";

		//Pass Names
		public const string OUTLINE_PASS_NAME = "OutlinePass";

		//Render Feature Names
		public const string OUTLINE_OPAQUE_RENDER_FEATURE_NAME = "Render Feature - Outline Opaque";
		public const string OUTLINE_TRANSPARENT_RENDER_FEATURE_NAME = "Render Feature - Outline Transparent";

		//Keys
		public const string KEY_LAST_PIPELINE_CONFIGURED = "PrefKey_AllIn1_LastPipelineConfigured";
	}
}