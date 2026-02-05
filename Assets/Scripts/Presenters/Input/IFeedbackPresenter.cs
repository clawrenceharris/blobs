

using Blobs.Core.Merge;


/// <summary>
/// Feedback service for displaying user feedback.
/// SRP: Only handles feedback display (visual/audio).
/// Decouples input/game logic from UI.
/// </summary>
public interface IFeedbackPresenter
    {
        /// <summary>Show generic feedback message</summary>
        void ShowFeedback(string message);

        /// <summary>Show feedback for failed selection</summary>
        void ShowCannotSelectFeedback(IBlobPresenter blob);

        /// <summary>Show feedback for same color merge attempt</summary>
        void ShowSameColorFeedback(IBlobPresenter blob);

        /// <summary>Show feedback for incompatible merge</summary>
        void ShowCannotMergeFeedback(IBlobPresenter blob);

        /// <summary>Show feedback when no target found</summary>
        void ShowNoTargetFeedback(IBlobPresenter blob);

        /// <summary>Show feedback when path is blocked</summary>
        void ShowPathBlockedFeedback(IBlobPresenter blob);

        /// <summary>Show feedback for move validation failure</summary>
        void ShowMoveValidationFeedback(IBlobPresenter blob, MergeFailReason result);
    }

