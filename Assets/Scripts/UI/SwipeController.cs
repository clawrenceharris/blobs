using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.EventSystems;
using DG.Tweening;
public class SwipeController : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private int maxPages;
    private int currentPage;
    private Vector3 targetPos;
    [SerializeField] private Vector3 pageStep;

    [SerializeField] private RectTransform pagesRect;
    [SerializeField] private float tweenTime;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button leftButton;


    private float dragThreshold;

        private void Awake()
    {
        currentPage = 1;
        targetPos = pagesRect.localPosition;
        dragThreshold = Screen.width / 15;
        UpdateArrowButton();

    }


    public void Next()
    {
        if(currentPage < maxPages)
        {
            currentPage++;
            targetPos += pageStep;
            MovePage();
        }
        UpdateArrowButton();
    }


    public void Previous()
    {
        if(currentPage > 1)
        {
            currentPage--;
            targetPos -= pageStep;
            MovePage();
        }
        UpdateArrowButton();
    }


    public void MovePage()
    {
        pagesRect.DOLocalMove(targetPos, tweenTime);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
       if(Mathf.Abs(eventData.position.x - eventData.pressPosition.x) > dragThreshold)
        {
            if (eventData.position.x > eventData.pressPosition.x) Previous();
            else Next();
        }

        else
        {
            MovePage();
        }
    }

    private void UpdateArrowButton()
    {
        if(currentPage == maxPages)
        {
            rightButton.interactable = false;
            
        }
        else
        {
            rightButton.interactable = true;
        }

        if (currentPage == 1)
        {
            leftButton.interactable = false;

        }
        else
        {
            leftButton.interactable = true;

        }
    }
}
