using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blobs.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(Button))]
    public class UiButtonView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text buttonLabel;
        [SerializeField] private UiThemeAsset theme;
        [SerializeField] private ButtonRole buttonRole = ButtonRole.Primary;
        [SerializeField] private ButtonShape buttonShape = ButtonShape.RoundedRect;
        [SerializeField] private Image buttonIcon;

        [SerializeField] private Button button;
        public Button Button => button;
        public UiThemeAsset Theme => theme;
        public Image ButtonIcon => buttonIcon;

        private void Awake()
        {

            ApplyState();
        }

        private void OnEnable() => ApplyState();

#if UNITY_EDITOR
        private void OnValidate() => ApplyState();
#endif

        public void ConfigureTheme(UiThemeAsset value, ButtonRole role = ButtonRole.Primary)
        {
            theme = value;
            buttonRole = role;
            ApplyState();
        }

        private void EnsureButton()
        {
            if (button == null)
                button = GetComponent<Button>();
        }


        public void SetEnabled(bool enabled)
        {
            EnsureButton();
            if (enabled)
            {
                button.interactable = true;
                ApplyState();
            }
            else
            {
                button.interactable = false;
                ApplyState();
            }
        }

        public void SetLabelText(string text)
        {
            buttonLabel.SetText(text);
        }



        public void OnPointerDown(PointerEventData eventData)
        {
            EnsureButton();
            if (button.interactable && theme != null && buttonLabel != null)
                buttonLabel.color = theme.GetButton(buttonRole).PressedLabel;
        }

        public void OnPointerUp(PointerEventData eventData) => ApplyLabelColor();
        public void OnPointerExit(PointerEventData eventData) => ApplyLabelColor();

        private void ApplyState()
        {
            if (theme == null)
                return;

            EnsureButton();
            Debug.Log($"Applying state for button: {button.image}");

            if (button.image != null)
            {
                button.image.color = button.interactable ? theme.GetButton(buttonRole).NormalFill : theme.GetButton(buttonRole).DisabledFill;
                button.image.sprite = theme.GetButtonSprite(buttonShape).NormalSprite;
                var spriteState = button.spriteState;
                spriteState.pressedSprite = theme.GetButtonSprite(buttonShape).PressedSprite;
                button.spriteState = spriteState;
            }

            ApplyLabelColor();
            ApplyIconColor();
        }

        private void ApplyLabelColor()
        {
            if (theme == null || buttonLabel == null)
                return;
            EnsureButton();
            if (buttonLabel.TryGetComponent(out UiThemeText _))
            {
                // Skip so we don't override the exisiting color
                return;
            }
            UiButtonTheme buttonTheme = theme.GetButton(buttonRole);
            buttonLabel.color = button.interactable
                ? buttonTheme.NormalLabel
                : buttonTheme.DisabledLabel;
        }

        private void ApplyIconColor()
        {
            if (theme == null || buttonIcon == null)
                return;
            EnsureButton();
            if (buttonIcon.TryGetComponent(out UiThemeText _))
            {
                // Skip so we don't override the exisiting color
                return;
            }
            buttonIcon.color = button.interactable ? theme.GetButton(buttonRole).NormalIcon : theme.GetButton(buttonRole).DisabledIcon;
        }
    }
}
