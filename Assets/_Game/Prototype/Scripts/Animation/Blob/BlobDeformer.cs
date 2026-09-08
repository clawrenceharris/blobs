using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class JellyDeformer : MonoBehaviour
{
    private static readonly int StretchAxisId = Shader.PropertyToID("_StretchAxis");
    private static readonly int StretchId     = Shader.PropertyToID("_Stretch");
    private static readonly int WobbleAmpId   = Shader.PropertyToID("_WobbleAmp");
    private static readonly int WobbleFreqId  = Shader.PropertyToID("_WobbleFreq");
    private static readonly int WobbleTimeId  = Shader.PropertyToID("_WobbleTime");
    private static readonly int ImpactId      = Shader.PropertyToID("_Impact");

    [Header("Defaults")]
    [SerializeField] private float travelStretch = 0.18f;
    [SerializeField] private float impactStretch = 0.28f;
    [SerializeField] private float wobbleAmp = 0.035f;
    [SerializeField] private float wobbleFreq = 14f;
    [SerializeField] private float wobbleDuration = 0.35f;
    [SerializeField] private float impactPulseDuration = 0.12f;

    private SpriteRenderer _sr;
    private MaterialPropertyBlock _mpb;

    private float _stretch;
    private float _wobbleAmp;
    private float _impact;
    private Vector2 _axis = Vector2.right;
    private float _wobbleTime;
    private Tween _wobbleTween;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _mpb = new MaterialPropertyBlock();
        Apply();
    }

    private void Update()
    {
        // Drive time deterministically (or you can drive via tween instead)
        _wobbleTime += Time.deltaTime;
        Apply();
    }

    private void Apply()
    {
        _sr.GetPropertyBlock(_mpb);
        _mpb.SetVector(StretchAxisId, _axis);
        _mpb.SetFloat(StretchId, _stretch);
        _mpb.SetFloat(WobbleAmpId, _wobbleAmp);
        _mpb.SetFloat(WobbleFreqId, wobbleFreq);
        _mpb.SetFloat(WobbleTimeId, _wobbleTime);
        _mpb.SetFloat(ImpactId, _impact);
        _sr.SetPropertyBlock(_mpb);
    }

    public void SetAxis(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        _axis = dir.normalized;
        Apply();
    }

    /// Call when blob starts moving toward target
    public void PlayTravelJelly()
    {
        KillTweens();

        _stretch = 0f;
        _wobbleAmp = 0f;
        _impact = 0f;

        // Stretch into motion
        DOTween.To(() => _stretch, v => _stretch = v, travelStretch, 0.12f)
            .SetEase(Ease.OutQuad);

        // Light wobble while traveling
        _wobbleTween = DOTween.To(() => _wobbleAmp, v => _wobbleAmp = v, wobbleAmp, 0.10f)
            .SetEase(Ease.OutQuad);
    }

    /// Call when blob hits target / merges
    public void PlayImpactJelly()
    {
        KillTweens();

        // quick pulse: increase stretch, then rebound past 0 slightly
        Sequence s = DOTween.Sequence();

        s.Append(DOTween.To(() => _impact, v => _impact = v, 1f, impactPulseDuration * 0.5f).SetEase(Ease.OutQuad));
        s.Append(DOTween.To(() => _impact, v => _impact = v, 0f, impactPulseDuration * 0.5f).SetEase(Ease.InQuad));

        s.Join(DOTween.To(() => _stretch, v => _stretch = v, impactStretch, impactPulseDuration * 0.5f).SetEase(Ease.OutQuad));
        s.Append(DOTween.To(() => _stretch, v => _stretch = v, -impactStretch * 0.35f, impactPulseDuration * 0.5f).SetEase(Ease.InOutQuad));
        s.Append(DOTween.To(() => _stretch, v => _stretch = v, 0f, 0.18f).SetEase(Ease.OutElastic, 0.9f, 0.25f));

        // wobble decays down
        _wobbleAmp = wobbleAmp * 1.25f;
        s.Join(DOTween.To(() => _wobbleAmp, v => _wobbleAmp = v, 0f, wobbleDuration).SetEase(Ease.OutQuad));

        s.Play();
    }

    public void ResetJelly(float duration = 0.1f)
    {
        KillTweens();
        DOTween.To(() => _stretch, v => _stretch = v, 0f, duration);
        DOTween.To(() => _wobbleAmp, v => _wobbleAmp = v, 0f, duration);
        DOTween.To(() => _impact, v => _impact = v, 0f, duration);
    }

    private void KillTweens()
    {
        _wobbleTween?.Kill();
        DOTween.Kill(this);
    }
}