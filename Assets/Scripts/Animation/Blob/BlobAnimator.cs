
using Blobs.Merge.Animation;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(BlobView))]
public class BlobAnimator : MonoBehaviour, IBlobAnimator
{

    protected BlobView blobView;
    protected SpriteRenderer _renderer;

    [SerializeField] private BlobAnimationRecipe _recipe;
    public BlobAnimationRecipe Recipe => _recipe;
    private Sequence _selectionLoop;
    private Vector3 _baseScale;


    private void Awake()
    {
        blobView = GetComponent<BlobView>();
    }
    
    private void Start()
    {
        _recipe = AnimationRecipeProvider.Instance.GetRecipe(blobView.Model.Type);
        _renderer = blobView.Visuals.SpriteRenderer;
        _baseScale = transform.localScale;
    }
    
    
    public virtual Tween CreateMoveTween(Vector3 worldTarget, float duration) =>
        transform.DOMove(worldTarget, duration).SetEase(Ease.OutQuad);

    public virtual Tween CreateScaleTween(float targetScale, float duration) =>
        transform.DOScale(targetScale, duration).SetEase(Ease.OutBack);

    public virtual Tween CreateRemoveTween(float duration) =>
        transform.DOScale(0f, duration).SetEase(Ease.InBack);

    public virtual Tween CreateSpawnTween(float duration)
    {
        transform.localScale = Vector3.zero;
        return transform.DOScale(1f, duration).SetEase(Ease.OutBack);
    }

    public virtual void StartSelectionLoop()
    {
        float selectionSquishDuration = _recipe.selectionSquishDuration;
        float selectionSquishAmount = _recipe.selectionSquishAmount;
        float selectionStretchAmount = _recipe.selectionStretchAmount;

       
        // Kill any existing loop
        StopSelectionLoop();
        
        // Store current scale as base (in case it was modified)
        _baseScale = transform.localScale;
        
        // Create squish in/out loop: squash down (Y smaller, X larger) then return to base
        _selectionLoop = DOTween.Sequence();
        
        Vector3 squishScale = new Vector3(
            _baseScale.x * selectionStretchAmount,
            _baseScale.y * selectionSquishAmount,
            _baseScale.z
        );
        
        // Squish in
        _selectionLoop.Append(transform.DOScale(squishScale, selectionSquishDuration)
            .SetEase(Ease.OutQuad));
        
        // Squish out (back to base)
        _selectionLoop.Append(transform.DOScale(_baseScale, selectionSquishDuration)
            .SetEase(Ease.InQuad));
        
        // Loop indefinitely
        _selectionLoop.SetLoops(-1, LoopType.Restart);
    }

    public virtual void StopSelectionLoop()
    {
        if (_selectionLoop != null && _selectionLoop.IsActive())
        {
            // Kill the loop and smoothly blend back to base scale
            _selectionLoop.Kill();
            transform.DOScale(_baseScale, _recipe.selectionSquishDuration * 0.5f).SetEase(Ease.OutQuad);
        }
        _selectionLoop = null;
    }
    
}