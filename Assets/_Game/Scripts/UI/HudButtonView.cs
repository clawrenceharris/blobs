using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    [RequireComponent(typeof(Button))]
    public class HudButtonView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        private Button _button;
        public Button Button => _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void Disable()
        {
            _button.interactable = false;
            var color = icon.color;
            color.a = 0.5f;
            icon.color = color;
        }

        public void Enable()
        {
            _button.interactable = true;
            var color = icon.color;
            color.a = 1f;
            icon.color = color;
        }


    }
}