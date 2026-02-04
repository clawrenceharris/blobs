using UnityEngine;


/// <summary>
/// Interface for blob visual representation (MonoBehaviour)
/// </summary>
public interface IBlobView
{
    Transform Transform { get; }
    
    void Initialize(Blob model);
    void UpdateVisual(BlobType type, BlobColor color);
    void SetPosition(Vector3 position);
        
    void Destroy();
}
