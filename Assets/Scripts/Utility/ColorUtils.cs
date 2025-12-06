

using UnityEngine;

public class ColorUtils
{
    /// <summary>
    /// Lightens a given color by a specified amount.
    /// </summary>
    /// <param name="color">The original color.</param>
    /// <param name="amount">The amount to lighten the color, where 0 means no change and 1 means fully white.</param>
    /// <returns>The lightened color.</returns>
    public static Color LightenColor(Color color, float amount)
    {
        // Clamp the amount to ensure it is between 0 and 1
        amount = Mathf.Clamp01(amount);

        // Interpolate between the original color and white
        return Color.Lerp(color, Color.white, amount);
    }

    /// <summary>
    /// Darkens a given color by a specified amount.
    /// </summary>
    /// <param name="color">The original color.</param>
    /// <param name="amount">The amount to lighten the color, where 0 means no change and 1 means fully black.</param>
    /// <returns>The darkened color.</returns>
    public static Color DarkenColor(Color color, float amount)
    {
        // Clamp the amount to ensure it is between 0 and 1
        amount = Mathf.Clamp01(amount);

        // Interpolate between the original color and white
        return Color.Lerp(color, Color.black, amount);
    }

    public static void ApplyColorsToMaterial(Material material, BlobColor color)
    {
        Color blobMainColor = ColorSchemeManager.FromBlobColor(color);

        material.SetColor("_BaseColor", blobMainColor);

        Color shadowColor = DarkenColor(blobMainColor, 0.3f);;
        material.SetColor("_ShadowColor", shadowColor);
        Color highlightColor = LightenColor(blobMainColor, 0.3f);;

        material.SetColor("_HighlightColor", highlightColor);
    }
}