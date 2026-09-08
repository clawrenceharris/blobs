using System;
using UnityEngine;

namespace Blobs.Content
{

    [Serializable]
    public sealed class BlobSelectionSettings
    {
        [SerializeField] private float selectionSquishDuration = 0.16f;
        [SerializeField] private float selectionSquishAmount = 1.065f;
        [SerializeField] private float selectionStretchAmount = 1.02f;

        public float SelectionSquishDuration => selectionSquishDuration;
        public float SelectionSquishAmount => selectionSquishAmount;
        public float SelectionStretchAmount => selectionStretchAmount;

        public void OnValidate()
        {
            selectionSquishDuration = Mathf.Max(0.01f, selectionSquishDuration);

            selectionSquishAmount = Mathf.Max(0.01f, selectionSquishAmount);
            selectionStretchAmount = Mathf.Max(0.01f, selectionStretchAmount);
        }

    }
    [Serializable]
    public sealed class BlobIdleSettings
    {
        [SerializeField] private float idleSquishDuration = 0.8f;
        [SerializeField] private float idleSquishAmount = 1.065f;
        [SerializeField] private float idleStretchAmount = 0.99f;

        public float IdleSquishDuration => idleSquishDuration;
        public float IdleSquishAmount => idleSquishAmount;
        public float IdleStretchAmount => idleStretchAmount;



        public void OnValidate()
        {
            idleSquishDuration = Mathf.Max(0.01f, idleSquishDuration);
            idleSquishAmount = Mathf.Max(0.01f, idleSquishAmount);
            idleStretchAmount = Mathf.Max(0.01f, idleStretchAmount);
        }

    }



    [Serializable]
    public sealed class BlobMotionSettings
    {
        [SerializeField, Min(0f)] private float spawnDuration = 0.14f;
        [SerializeField, Min(0f)] private float despawnDuration = 0.12f;



        [SerializeField] private float moveDuration = 0.25f;
        public float MoveDuration => moveDuration;
        public float SpawnDuration => spawnDuration;
        public float DespawnDuration => despawnDuration;
        public void OnValidate()
        {
            moveDuration = Mathf.Max(0.01f, moveDuration);
            spawnDuration = Mathf.Max(0.01f, spawnDuration);
            despawnDuration = Mathf.Max(0.01f, despawnDuration);
        }
    }

    [Serializable]
    public sealed class BlobMergeImpactSettings
    {
        [Header("Timing")]
        [SerializeField] private float _anticipationDuration = 0.0f;
        [SerializeField] private float _travelDuration = 0.4f;
        [SerializeField] private float _consumeDuration = 0.2f;
        [SerializeField] private float _settleDuration = 0.4f;

        [Header("Deformation")]
        [SerializeField, Min(0f)] private float _anticipationBackstep = 0.08f;
        [SerializeField] private Vector2 _anticipationScale = new(1.10f, 0.88f);
        [SerializeField] private Vector2 _travelScale = new(0.9f, 1.056f);
        [SerializeField] private Vector2 _targetBraceScale = new(0.88f, 0.99f);
        [SerializeField] private Vector2 _survivorImpactScale = new(1.14f, 1.28f);

        [Header("Merge Sorting")]
        [SerializeField] private int _sourceSortingOffset = 10;
        [SerializeField] private int _targetSortingOffset = 20;

        public float AnticipationDuration => _anticipationDuration;
        public float TravelDuration => _travelDuration;
        public float ConsumeDuration => _consumeDuration;
        public float SettleDuration => _settleDuration;

        public float AnticipationBackstep => _anticipationBackstep;
        public Vector2 AnticipationScale => _anticipationScale;
        public Vector2 TravelScale => _travelScale;
        public Vector2 TargetBraceScale => _targetBraceScale;

        public int SourceSortingOffset => _sourceSortingOffset;
        public int TargetSortingOffset => _targetSortingOffset;
        public Vector2 SurvivorImpactScale => _survivorImpactScale;
        public void OnValidate()
        {
            _anticipationDuration = Mathf.Max(0.01f, _anticipationDuration);
            _travelDuration = Mathf.Max(0.01f, _travelDuration);
            _consumeDuration = Mathf.Max(0.01f, _consumeDuration);
            _settleDuration = Mathf.Max(0.01f, _settleDuration);
            _anticipationBackstep = Mathf.Max(0.01f, _anticipationBackstep);
            _sourceSortingOffset = Mathf.Max(1, _sourceSortingOffset);
            _targetSortingOffset = Mathf.Max(1, _targetSortingOffset);
            _anticipationScale = new Vector2(Mathf.Max(0.01f, _anticipationScale.x), Mathf.Max(0.01f, _anticipationScale.y));
            _travelScale = new Vector2(Mathf.Max(0.01f, _travelScale.x), Mathf.Max(0.01f, _travelScale.y));
            _targetBraceScale = new Vector2(Mathf.Max(0.01f, _targetBraceScale.x), Mathf.Max(0.01f, _targetBraceScale.y));
            _survivorImpactScale = new Vector2(Mathf.Max(0.01f, _survivorImpactScale.x), Mathf.Max(0.01f, _survivorImpactScale.y));
        }

    }

    [Serializable]
    public sealed class GhostReturnSettings
    {
        [SerializeField] private float _moveDuration = 0.25f;
        [SerializeField] private float _despawnDuration = 0.12f;
        [SerializeField] private float _fadeDuration = 0.15f;

        public float MoveDuration => _moveDuration;
        public float DespawnDuration => _despawnDuration;

        public float FadeDuration => _fadeDuration;

    }

    [CreateAssetMenu(fileName = "BlobAnimationSettings_New", menuName = "Blobs/Animation/Blob Animation Settings")]
    public class BlobAnimationSettingsAsset : ScriptableObject
    {

        public BlobSelectionSettings BlobSelectionSettings;
        public BlobIdleSettings BlobIdleSettings;
        public BlobMergeImpactSettings MergeImpactSettings;
        public BlobMotionSettings BlobMotionSettings;
        public GhostReturnSettings GhostReturnSettings;

        private void OnValidate()
        {
            BlobSelectionSettings.OnValidate();
            BlobIdleSettings.OnValidate();
            BlobMotionSettings.OnValidate();
            MergeImpactSettings.OnValidate();

        }
    }



}
