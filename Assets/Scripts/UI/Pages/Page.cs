using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


public enum EntryMode
    {

        Fade,
        None,
        Slide,
        

    }

public enum EntryDirection
{

    Left,
    Right,
    Up,
    None ,
    Down,  
     
}

/// <summary>
/// This class represents a UI page in the game
/// </summary>
[RequireComponent(typeof(CanvasGroup))]

public class  Page : MonoBehaviour
{


    protected IPageVisualController visualController;

    public T GetVisualController<T>() where T : class
        {
            if (visualController is T controller)
            {
                return controller;
            }
            throw new InvalidCastException($"Unable to cast base visualController of type {visualController.GetType().Name} to {typeof(T).Name}");
        }
    public PageVisualController VisualController => GetVisualController<PageVisualController>();

    private readonly float animationSpeed = 0.3f;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    [SerializeField]private RectTransform initialTransform; 
    [SerializeField]private float exitDelay;
    [SerializeField]private float entryDelay;
    [SerializeField] private PageType pageType;
    public PageType PageType => pageType;
   [SerializeField] private EntryMode entryMode;
   [SerializeField] private EntryMode exitMode;
    [SerializeField] private EntryDirection entryDirection;
   [SerializeField] private EntryDirection exitDirection;
    public virtual EntryMode EntryMode { get => entryMode; }
    public virtual EntryMode ExitMode  { get => exitMode;  }
    public virtual EntryDirection EntryDirection{  get => entryDirection; }
    public virtual EntryDirection ExitDirection{get => exitDirection; }

    private Coroutine AnimationCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        Init();

    }
    public virtual void Init()
    {
        rectTransform.position = initialTransform.position;
        InitVisuals();
    }
    public virtual void InitVisuals()
    {
        visualController = new PageVisualController();
        visualController.Init(this);
    }
    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
   
    /// <summary>
    /// Starts the entry animation to show the page
    /// </summary>
    public void Enter()
    {
        StartCoroutine(EntryAnimation());
    }



   private IEnumerator ExitAnimation(Action onComplete)
    {
        if (AnimationCoroutine != null)
        {
            StopCoroutine(AnimationCoroutine);
        }

        yield return new WaitForSeconds(exitDelay);

        switch (ExitMode)
        {
            case EntryMode.Fade:
                AnimationCoroutine = StartCoroutine(PageAnimationHelper.FadeOut(canvasGroup, animationSpeed, null));
                break;
            case EntryMode.Slide:
                AnimationCoroutine = StartCoroutine(PageAnimationHelper.SlideOut(rectTransform, ExitDirection, animationSpeed, null));
                break;
        }

        yield return AnimationCoroutine;
        rectTransform.anchoredPosition = initialTransform.position;
        onComplete?.Invoke();
    }

    private IEnumerator EntryAnimation()
    {
        if (AnimationCoroutine != null)
        {
            StopCoroutine(AnimationCoroutine);
        }

        yield return new WaitForSeconds(entryDelay);
        switch (EntryMode)
        {
            case EntryMode.Fade:
                AnimationCoroutine = StartCoroutine(PageAnimationHelper.FadeIn(canvasGroup, animationSpeed, null));
                break;
            case EntryMode.Slide:
                AnimationCoroutine = StartCoroutine(PageAnimationHelper.SlideIn(rectTransform, EntryDirection, animationSpeed, null));
                break;


        }
        yield return AnimationCoroutine;



    }
    /// <summary>
    /// Starts the exit animation to remove the page from view
    /// </summary>
    public void Exit(Action onComplete = null)
    {
        StartCoroutine(ExitAnimation(onComplete));

    }

   
}