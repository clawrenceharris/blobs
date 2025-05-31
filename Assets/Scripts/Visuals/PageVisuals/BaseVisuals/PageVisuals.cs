using UnityEngine;
using UnityEngine.UI;

public class PageVisuals : MonoBehaviour, IPageVisuals
{
    public Image[] Panels { get => panels; }
    [SerializeField]private Image[] panels;
 
}