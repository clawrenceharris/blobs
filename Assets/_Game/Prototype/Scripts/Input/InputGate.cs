using UnityEngine;


namespace Blobs.Input
{

    public class InputGate : IInputGate
    {
        public bool Enabled { get; private set; }
        public void SetEnabled(bool enabled) => Enabled = enabled;
        
    }
}