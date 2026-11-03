using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>Fades an explicitly authored group, preserving each renderer's original opacity.</summary>
    public sealed class MaskableVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] renderers;

        public void ApplyMaskInteraction(SpriteMaskInteraction interaction)
        {
            foreach (var renderer in renderers)
            {
                renderer.maskInteraction = enabled ? interaction : SpriteMaskInteraction.None;
            }
        }



    }
}
