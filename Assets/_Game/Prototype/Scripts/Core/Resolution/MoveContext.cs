using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolver-level context computed once; may be adjusted by rules (ice/portals/sticky).
/// </summary>
public sealed class MoveContext
{
    public IBoardPresenter Board;
    public Blob Source;
    public Blob Target;
    public CellContext Start;
    public CellContext End;
    public Vector2Int Direction;
    public List<CellContext> Path = new();
    public string MoveTag = "Normal";
    public bool Terminate;
}

/// <summary>
/// Single cell along the move path.
/// </summary>
public readonly struct CellContext
{
    public readonly Vector2Int Pos;
    public readonly Tile Tile;
    public readonly Blob Blob;

    public CellContext(Vector2Int pos, Tile tile, Blob blob)
    {
        Pos = pos;
        Tile = tile;
        Blob = blob;
    }
    public static CellContext Create( IBoardPresenter board, Vector2Int pos, Blob blob = null)
    {
        var tile = board.GetTileAt(pos)?.Model;
        return new CellContext(pos, tile, blob ?? board.GetBlobAt(pos)?.Model);
    }

   
}