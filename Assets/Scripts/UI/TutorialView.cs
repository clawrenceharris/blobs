using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class TutorialView : MonoBehaviour
{

    [SerializeField] private GameObject _tutorialPointerPrefab;
    public GameObject TutorialPointerPrefab => _tutorialPointerPrefab;
    private CanvasGroup _canvasGroup;

    [SerializeField] private TextMeshProUGUI _topText;

    [SerializeField] private TextMeshProUGUI _bottomText;


    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowTutorialView()
    {
        StartCoroutine(FadeIn());
    }

    public void HideTutorialView()
    {
        StartCoroutine(FadeOut());
    }


    public IEnumerator UpdateTutorialText(string topText, string bottomText)
    {
        yield return FadeOut();
        _topText.text = topText;
        _bottomText.text = bottomText;
        yield return FadeIn();
    }
    private IEnumerator FadeIn(float duration = 0.6f)
    {
        _canvasGroup.gameObject.SetActive(true);
        yield return _canvasGroup.DOFade(1, duration).WaitForCompletion();
    }
    private IEnumerator FadeOut(float duration = 0.6f)
    {

        yield return _canvasGroup.DOFade(0, duration).WaitForCompletion();
        _canvasGroup.gameObject.SetActive(false);
    }


}