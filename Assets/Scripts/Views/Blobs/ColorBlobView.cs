using ColorUtility = Blobs.Utilities.ColorUtility;

public abstract class ColorBlobView : BlobView
{
    public override void Initialize(Blob model)
    {
        base.Initialize(model);
        // Apply the color to the sprite renderer's material.
        ColorUtility.ApplyColorsToMaterial(Visuals.SpriteRenderer.material, model.Color);
    }
}