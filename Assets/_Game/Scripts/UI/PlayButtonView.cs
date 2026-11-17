using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    [RequireComponent(typeof(Button))]
    public class PlayButtonView : MonoBehaviour
    {
        [SerializeField] private TMP_Text buttonLabel;
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private Color enabledColor = Color.green;

        [SerializeField] private Color disabledTextColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        private Button _button;
        public Button Button => _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void Disable()
        {
            _button.interactable = false;
            buttonLabel.color = disabledTextColor;
            _button.image.color = disabledColor;
        }

        public void Enable()
        {
            _button.interactable = true;
            buttonLabel.color = Color.white;
            _button.image.color = enabledColor;
        }

        public void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                Enable();
            }
            else
            {
                Disable();
            }
        }

        public void SetLabel(string label)
        {
            buttonLabel.SetText(label);
        }


    }
}