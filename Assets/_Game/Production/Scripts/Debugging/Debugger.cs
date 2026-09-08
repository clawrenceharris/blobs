using System;

namespace Blobs.Debugging
{

    public sealed class Debugger
    {
        private static Action<string> LogMessage;
        public Debugger(Action<string> logMessage)
        {
            LogMessage = logMessage;
        }
        public static void Log(string message)
        {
            LogMessage?.Invoke(message);
        }
    }
}