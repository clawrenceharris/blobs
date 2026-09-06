using UnityEngine;
namespace Blobs.Presentation
{
    public interface ILogger
    {
        void Log(string message);

    }

    public sealed class Logger : ILogger
    {
        public void Log(string message)
        {
            Debug.Log(message);
        }
    }
}

