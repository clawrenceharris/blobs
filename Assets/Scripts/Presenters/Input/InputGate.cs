using UnityEngine;


namespace Blobs.Input
{

    public class InputGate: MonoBehaviour
    {
        public bool Enabled { get; private set; }
        public void SetEnabled(bool enabled) => Enabled = enabled;
        
    }
}