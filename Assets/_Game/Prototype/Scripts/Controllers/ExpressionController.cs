using System;
using System.Linq;
using UnityEngine;
public enum ExpressionType
{
    Normal,
    Merging,
    Sad,
    Angry,
    Surprised,
}
public class ExpressionController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Serializable]
    public struct Expression
    {
        public Sprite Sprite;
        public ExpressionType Type;
    }
    [SerializeField] private Expression[] _expressions;
    
    private void OnEnable()
    {
        ShowExpression(ExpressionType.Normal);
    }
    public void ShowExpression(ExpressionType type)
    {
        if (_expressions.FirstOrDefault(e => e.Type == type) is { } expression)
        {
            _spriteRenderer.sprite = expression.Sprite;
        }

        else
        {
            Debug.LogWarning($"Expression {type} not found");
        }
    }
    public void RemoveExpression()
    {
        _spriteRenderer.sprite = null;
    }
}