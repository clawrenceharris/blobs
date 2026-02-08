
using System;
using UnityEngine;

public interface IBlobModel
{
    string ID { get; }
    bool Enabled { get; }
    BlobType Type { get; }
    BlobColor Color { get; }
    Vector2Int GridPosition { get; set; }
    BlobSize Size { get; }
    float GetScaleFromBlobSize();
    void EnableBlob();
    void DisableBlob();
}
