using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Type;
public class GameAssets : MonoBehaviour
{
    public static GameAssets Instance { get; private set; }
    public BlobsGameObject NormalTile;
    public BlobsGameObject SpikeTile;
    public BlobsGameObject LaserTile;
    public BlobsGameObject NormalBlob;
    public BlobsGameObject SwitchBlob;
    public BlobsGameObject FlagBlob;
    public BlobsGameObject TargetTile;

    public BlobsGameObject TrailBlob;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
 
        }
    }

    public BlobsGameObject FromBlobType(BlobType type)
    {
        return type switch
        {
            BlobType.Normal => NormalBlob,
            BlobType.Trail => TrailBlob,
            BlobType.Flag => FlagBlob,
            BlobType.Switch => SwitchBlob,

            _ => throw new ArgumentException("No type matches given type: " + type),
        };
    }
     public BlobsGameObject FromTileType(TileType type)
    {
        return type switch
        {
            TileType.Normal => NormalTile,
            TileType.Target => TargetTile,
            TileType.Spike => SpikeTile,

            _ => throw new ArgumentException("No type matches given type"),
        };
    }
}

