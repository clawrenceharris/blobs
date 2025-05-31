using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public enum PageType
{
    MainMenu,
    GameOver,
    LevelComplete,
    Level,
    LevelSelect,
    Credits,

    Pause
}

/// <summary>
/// This class manages the UI pages in the game
/// </summary>
public class MenuController : MonoBehaviour
{

    public static Stack<Page> Pages { get; private set; } = new();
    private Page currentPage => Pages.Peek();
    [SerializeField] private Page initialPage;
    public static List<Page> AllPages { get; private set; }
    public static MenuController Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        AllPages = new();
        if (initialPage != null)
        {

            PushPage(initialPage);
        }

        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<Page>(out var page))
            {
                AllPages.Add(page);
            }
        }
    }

    /// <summary>
    /// Resets the UI by clearing the stack, exiting all pages and pushing the initial page
    /// </summary>
    public void PopAll()
    {
        foreach (Page _ in Pages)
        {
            PopPage();
        }
        PushPage(initialPage);
    }

    /// <summary>
    /// Pushes a page to the stack and activates it given the page type
    /// </summary>
    /// <param name="pgae"></param>
    public void PushPage(PageType page)
    {
        Page pageToPush = AllPages.FirstOrDefault(s => s.PageType == page);
        PushPage(pageToPush);

    }




    public void PushPage(Page page)
    {

        Pages.Push(page);
        page.Enter();
    }




    /// <summary>
    /// Pops a page from the stack and deactivates it
    /// </summary>
    public void PopPage()
    {
        if (Pages.Count > 0)
        {
            Page page = Pages.Pop();
            page.Exit();
        }


    }

    public void ReplacePage(Page page)
    {
        PopPage();
        PushPage(page);
    }

      public void ReplacePage(PageType page)
    {
        PopPage();
        Page pageToPush = AllPages.FirstOrDefault(s => s.PageType == page);
        PushPage(pageToPush);
    }

   
  

}