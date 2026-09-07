using System;
using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns player-facing copy and visibility policy for move failures. Keeping this authored data
    /// outside Core lets presentation evolve or localize without changing gameplay rules.
    /// </summary>
    [CreateAssetMenu(
        fileName = "FailureFeedbackCatalog",
        menuName = "Blobs/Presentation/Failure Feedback Catalog")]
    public sealed class FailureFeedbackCatalogAsset : ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            [SerializeField] private MoveFailureReason reason;
            [SerializeField] private bool showFeedback;
            [SerializeField, TextArea] private string message;

            public MoveFailureReason Reason => reason;
            public bool ShowFeedback => showFeedback;
            public string Message => message;
        }

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<MoveFailureReason, Entry> _lookup;

        /// <summary>
        /// Resolves visible player-facing feedback. A configured silent entry deliberately returns
        /// false, allowing selection-stage rejections to remain unobtrusive.
        /// </summary>
        public bool TryGetVisibleMessage(MoveFailureReason reason, out string message)
        {
            EnsureLookup();

            if (_lookup.TryGetValue(reason, out Entry entry) &&
                entry.ShowFeedback &&
                !string.IsNullOrWhiteSpace(entry.Message))
            {
                message = entry.Message;
                return true;
            }

            message = string.Empty;
            return false;
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<MoveFailureReason, Entry>();
            foreach (Entry entry in entries)
            {
                if (entry == null)
                    continue;

                if (!_lookup.TryAdd(entry.Reason, entry))
                {
                    throw new InvalidOperationException(
                        $"Duplicate failure feedback registration for {entry.Reason}.");
                }
            }
        }

        private void OnEnable()
        {
            _lookup = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _lookup = null;
            var registeredReasons = new HashSet<MoveFailureReason>();

            foreach (Entry entry in entries)
            {
                if (entry == null)
                    continue;

                if (!registeredReasons.Add(entry.Reason))
                {
                    Debug.LogError(
                        $"Failure reason {entry.Reason} is registered more than once.",
                        this);
                }

                if (entry.ShowFeedback && string.IsNullOrWhiteSpace(entry.Message))
                {
                    Debug.LogError(
                        $"Visible failure feedback for {entry.Reason} requires a message.",
                        this);
                }
            }

            foreach (MoveFailureReason reason in Enum.GetValues(typeof(MoveFailureReason)))
            {
                if (!registeredReasons.Contains(reason) && reason != MoveFailureReason.None)
                {
                    Debug.LogError(
                        $"Failure reason {reason} is not registered in the feedback catalog.",
                        this);
                }
            }
        }
#endif
    }
}
