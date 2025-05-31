using System;
using UnityEngine;

public abstract class BlobsGameObject : MonoBehaviour
{
    public Position Position {get; protected set;}
    protected IVisualController visualController;

    public T GetVisualController<T>() where T : class
    {
        if (visualController is T controller)
        {
            return controller;
        }
        UnityEngine.Debug.Log($"VisualController is not of type {typeof(T)}");
        return null;
    }
    public virtual void Init(Position position)
    {
        Position = position;
        InitVisuals();

    }

    protected abstract void InitVisuals();

}