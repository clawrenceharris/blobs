using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Effect: set a tile state key (e.g. laser IsActive). For Phase 9 laser/switch integration.
/// </summary>
public readonly struct SetTileStateEffect : IEffect
{
    public readonly string TileId;
    public readonly string Key;
    public readonly object From;
    public readonly object To;
    public readonly Vector2Int At;
    public readonly Dictionary<string, object> Data;

    public T GetProperty<T>(string key)
    {
        if (Data.ContainsKey(key))
        {
            return (T)Data[key];
        }
        return default;
    }

    public void SetProperty<T>(string key, T value)
    {
        if (Data.ContainsKey(key))
        {
            Data[key] = value;
        }
        else
        {
            Data.Add(key, value);
        }
    }
    public SetTileStateEffect(string tileId, string key, object from, object to, Vector2Int at, Dictionary<string, object> data = null)
    {
        TileId = tileId;
        Key = key;
        From = from;
        To = to;
        At = at;
        Data = data ?? new Dictionary<string, object>();
    }
}
