using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class PanelView : MonoBehaviour
{
    protected CanvasGroup _canvasGroup;
    protected RectTransform _rectTransform;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
    }
    public virtual void ShowPanel()
        {
            // Activate first so Awake() runs if the panel started inactive,
            // ensuring _canvasGroup and _rectTransform are initialized.
            gameObject.SetActive(true);
            Time.timeScale = 0f;

            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, 0.3f).SetUpdate(true); // SetUpdate(true) ignores timeScale

            
            _rectTransform.localScale = Vector3.one * 0.9f;
            _rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
            
        }

         public void HidePanel()
        {

            Time.timeScale = 1f;

            // Animate
            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() =>
                {
                    _canvasGroup.gameObject.SetActive(false);
                });
            }
            else
            {
                _canvasGroup.gameObject.SetActive(false);
            }
        }

}