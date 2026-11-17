using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace AllIn1Assets
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private KeyCode targetKey = KeyCode.A;
        [SerializeField] private KeyCode altKey = KeyCode.None;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private Key targetNewInputKey;
        private Key altNewInputKey;
        
        private void Awake()
        {
            targetNewInputKey = InputKeyConverter.GetKeyFromKeycode(targetKey);
            altNewInputKey = InputKeyConverter.GetKeyFromKeycode(altKey);
        }
#endif

        public bool IsKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if(Keyboard.current == null) return false;

            if(targetKey != KeyCode.None && Keyboard.current[targetNewInputKey].wasPressedThisFrame) return true;
            return altKey != KeyCode.None && Keyboard.current[altNewInputKey].wasPressedThisFrame;
#else
            if(targetKey != KeyCode.None && Input.GetKeyDown(targetKey)) return true;
            return altKey != KeyCode.None && Input.GetKeyDown(altKey);
#endif
        }

        public KeyCode GetTargetKey() => targetKey;
        public KeyCode GetAltKey() => altKey;
    }
}