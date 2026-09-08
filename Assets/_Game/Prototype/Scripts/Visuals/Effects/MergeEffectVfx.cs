using DG.Tweening;
using UnityEngine;

namespace Blobs.Visuals
{
    /// <summary>
    /// Composes merge visual effects: particles and shock wave.
    /// Spawn at merge position, set color from blob, play all effects, then destroy.
    /// Each blob type can reference its own MergeEffect prefab via BlobVisuals.
    /// </summary>
    public class MergeEffectVfx : MonoBehaviour, IMergeEffectVfx
    {
        [Header("Particles")]
        [SerializeField] private ParticleSystem[] _particles;

        [Header("Shock Wave")]
        [SerializeField] private ShockWaveEffect _shockWave;
        [SerializeField] private float _shockWaveDuration = 0.4f;
        [SerializeField] private float _shockWaveEndScale = 3f;
        [SerializeField] private AnimationCurve _shockWaveScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Lifecycle")]
        [SerializeField] private float _destroyDelay = 2f;

        private bool _isPlaying;

        private void Reset()
        {
            _shockWaveDuration = 0.4f;
            _shockWaveEndScale = 3f;
            _destroyDelay = 2f;
        }

        /// <summary>
        /// Play the merge effect at the given position with the given color.
        /// Typically called after Instantiate(prefab, position, Quaternion.identity).
        /// </summary>
        public Sequence Play(Vector3 position, Color color)
        {
            if (_isPlaying) return null;
            _isPlaying = true;
            Sequence sequence = DOTween.Sequence();

            transform.position = position;
            if (_shockWave != null){
                PlayShockWave(color);
            }
            SetupParticles(color);
            PlayParticles();
            

            float maxDuration = Mathf.Max(_shockWaveDuration, GetParticlesMaxLifetime());
                sequence.AppendInterval(Mathf.Max(maxDuration, _destroyDelay)).OnComplete(() =>
                {
                    Destroy(gameObject);
                _isPlaying = false;
            });
            return sequence;
        }

        /// <summary>
        /// Play with BlobColor (converts via ColorSchemeManager). Call from presenters.
        /// </summary>
        public Sequence PlayWithBlobColor(Vector3 position, BlobColor blobColor)
        {
            var color = ColorSchemeManager.FromBlobColor(blobColor);
            return Play(position, color);
        }

        private void SetupParticles(Color color)
        {
           
            if (_particles == null) return;

            foreach (var ps in _particles)
            {
                if (ps == null) continue;
                var main = ps.main;
                main.startColor = color;
            }
        }

        private void PlayParticles()
        {
            if (_particles == null) return;
            foreach (var ps in _particles)
            {
                if (ps != null)
                {
                    ps.Play();

                }
                   
            }
        }

        private Sequence PlayShockWave(Color color)
        {
            return _shockWave.Play(color, _shockWaveDuration, _shockWaveEndScale, _shockWaveScaleCurve);
        }

        private float GetParticlesMaxLifetime()
        {
            float max = 0f;
            if (_particles == null) return max;
            foreach (var ps in _particles)
            {
                if (ps == null) continue;
                var main = ps.main;
                float duration = main.duration + main.startLifetime.constantMax;
                if (duration > max) max = duration;
            }
            return max;
        }
    }
}
