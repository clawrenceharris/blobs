using System;
using System.Net.NetworkInformation;
using UnityEngine;
using Color = UnityEngine.Color;

public class ColorSchemeManager : MonoBehaviour
{
    [SerializeField]private ColorScheme[] colorSchemes;
    public static ColorScheme CurrentColorScheme { get; private set; }
    
    private void Start()
    {
        SetColorScheme(0);
    }

    public void SetColorScheme(int index)
    {
        if (index >= 0 && index < colorSchemes.Length)
        {
            CurrentColorScheme = colorSchemes[index];
        }
        else
        {
            Debug.LogError("Invalid color scheme index.");
        }
    }


    public static Color FromBlobColor(BlobColor color)
    {
        return color switch
        {
            BlobColor.Red => CurrentColorScheme.Red,
            BlobColor.Yellow => CurrentColorScheme.Yellow,
            BlobColor.Green => CurrentColorScheme.Green,
            BlobColor.Blue => CurrentColorScheme.Blue,
            BlobColor.Purple => CurrentColorScheme.Purple,
            BlobColor.LightBlue => CurrentColorScheme.Blue,
            BlobColor.Pink => CurrentColorScheme.Pink,


            _ => CurrentColorScheme.Blank,
        };
    }

}

