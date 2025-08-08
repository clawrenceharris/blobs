using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameAssets : MonoBehaviour
{
    public static GameAssets Instance { get; private set; }
    
    [Header("Blobs")]
    public BlobView NormalBlob;
    public BlobView SwitchBlob;
    public BlobView FlagBlob;
    public BlobView TrailBlob;
    public BlobView BombBlob;
    public BlobView GhostBlob;
    public BlobView EnemyBlob;
    public BlobView RockBlob;

    [Header("Tiles")]

    public TileView NormalTile;
    public TileView SpikeTile;
    public TileView LaserTile;
    public TileView TargetTile;
    public TileView SigilTile;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

        }
    }

    public BlobView FromBlobType(BlobType type)
    {
        return type switch
        {
            BlobType.Normal => NormalBlob,
            BlobType.Trail => TrailBlob,
            BlobType.Flag => FlagBlob,
            BlobType.Switch => SwitchBlob,
            BlobType.Bomb => BombBlob,
            BlobType.Ghost => GhostBlob,
            BlobType.Enemy => EnemyBlob,
            BlobType.Rock => RockBlob,

            _ => throw new ArgumentException("No type matches given type: " + type),
        };
    }
     public TileView FromTileType(TileType type)
    {
        return type switch
        {
            TileType.Normal => NormalTile,
            TileType.Target => TargetTile,
            TileType.Spike => SpikeTile,
            TileType.Sigil => SigilTile,
            TileType.Laser => LaserTile,

            _ => throw new ArgumentException("No type matches given type"),
        };
    }
}

