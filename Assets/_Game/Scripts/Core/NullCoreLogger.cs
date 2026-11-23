namespace Blobs.Core
{
    public interface ICoreLogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }

    public sealed class NullCoreLogger : ICoreLogger
    {
        public static readonly NullCoreLogger Instance = new();

        private NullCoreLogger() { }

        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }
}