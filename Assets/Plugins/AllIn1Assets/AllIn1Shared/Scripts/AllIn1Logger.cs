using UnityEngine;

namespace AllIn1Assets
{
	public static class AllIn1Logger
	{
		[System.Diagnostics.Conditional("ALLIN1_LOG")]
		public static void Log(string tag, string message)
		{
			Debug.Log($"{tag} # {message}");
		}

		[System.Diagnostics.Conditional("ALLIN1_LOG")]
		public static void Log(string message)
		{
			Log(string.Empty, message);
		}


		[System.Diagnostics.Conditional("ALLIN1_LOG")]
		public static void LogWarning(string tag, string message)
		{
			Debug.LogWarning($"{tag} # {message}");
		}

		[System.Diagnostics.Conditional("ALLIN1_LOG")]
		public static void LogWarning(string message)
		{
			LogWarning(string.Empty, message);
		}

		[System.Diagnostics.Conditional("ALLIN1_LOG")]
		public static void LogError(string tag, string message)
		{
			Debug.LogError($"{tag} # {message}");
		}

		[System.Diagnostics.Conditional("ALLIN1_LOG")]
		public static void LogError(string message)
		{
			LogError(string.Empty, message);
		}
	}
}