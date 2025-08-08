using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "ColorScheme", menuName = "Scriptable Objects/Color Scheme")]
public class ColorScheme : ScriptableObject
{
    public Color Purple;
        public Color Pink;

    public Color TileColor;
    public Color TileEdgeColor;
    public Color BackgroundColor;
    public Color Yellow;
    public Color Blue;
    public Color Green;
    public Color Red;
    public Color Blank;
}
