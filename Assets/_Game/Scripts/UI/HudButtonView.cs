using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    public class HudButtonView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Button button;
        public Button Button => button;

        public void Disable()
        {
            button.interactable = false;
            var color = icon.color;
            color.a = 0.5f;
            icon.color = color;
        }

        public void Enable()
        {
            button.interactable = true;
            var color = icon.color;
            color.a = 1f;
            icon.color = color;
        }


    }
}