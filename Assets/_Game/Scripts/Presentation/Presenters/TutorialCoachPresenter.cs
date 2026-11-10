using System;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Choreographs each tutorial transition on the shared board from the asset's timing: hold on the
    /// result, fade the old captions and pointer, dim or restore the board, then reveal new pieces,
    /// stagger the captions in, unlock input, and start the ghost swipe after its delay. Reaction
    /// slides show centered text and arm tap-to-continue. After the last step the captions and board
    /// fade out and <see cref="OutroCompleted"/> fires. The sequencer decides when steps change;
    /// this presenter only shows them.
    /// </summary>
    internal sealed class TutorialCoachPresenter : IDisposable
    {
        private readonly CoachPointerView _view;
        private readonly BoardPresenter _board;
        private readonly TutorialSequencer _sequencer;
        private readonly IGameplayState _state;
        private readonly TutorialSequenceAsset _asset;
        private Sequence _transition;
        private TutorialStepChange _current;
        private bool _hasPresented;

        /// <param name="view">Optional. Without it the board still reveals and fades, and reactions auto-advance.</param>
        public TutorialCoachPresenter(
            CoachPointerView view,
            BoardPresenter board,
            TutorialSequencer sequencer,
            IGameplayState state,
            TutorialSequenceAsset asset)
        {
            _view = view;
            _board = board != null ? board : throw new ArgumentNullException(nameof(board));
            _sequencer = sequencer ?? throw new ArgumentNullException(nameof(sequencer));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _asset = asset != null ? asset : throw new ArgumentNullException(nameof(asset));

            if (_view != null)
                _view.HideImmediately();
            _sequencer.StepChanged += HandleStepChanged;
            _sequencer.SequenceCompleted += HandleSequenceCompleted;
            _state.StateRestored += HandleStateRestored;
        }

        public void Dispose()
        {
            _sequencer.StepChanged -= HandleStepChanged;
            _sequencer.SequenceCompleted -= HandleSequenceCompleted;
            _state.StateRestored -= HandleStateRestored;
            KillTransition(complete: false);
            ResetBoard();
            if (_view != null)
                _view.HideImmediately();
        }

        private void HandleStepChanged(TutorialStepChange change)
        {
            // Set first so callbacks of the transition being completed see they are stale.
            _current = change;
            // Snap any unfinished entrance to its end so the next exit starts from a settled board.
            KillTransition(complete: true);
            if (_view != null)
                _view.DisarmContinue();

            TutorialStep step = change.Step;
            TutorialTimingAssetData timing = _asset.GetTiming(change.Index);
            bool first = !_hasPresented;
            _hasPresented = true;

            if (step.IsReaction || timing.lockInputUntilEntranceEnds)
                _board.SetInteractionLocked(true);

            Sequence transition = DOTween.Sequence().SetLink(_board.gameObject, LinkBehaviour.KillOnDestroy);
            _transition = transition;

            if (!first)
            {
                transition.AppendInterval(timing.holdBeforeTransition);
                if (change.RemovedBlobIds.Count > 0)
                    transition.Insert(
                        transition.Duration(),
                        _board.CreateRemovalSequence(change.RemovedBlobIds, timing.removeDuration));
                AppendExit(transition, step, timing);
                transition.AppendInterval(timing.gapAfterFadeOut);
            }
            else if (_board.BoardAlpha < 1f)
            {
                // Handed off from a previous tutorial that faded the board out.
                transition.Append(_board.FadeBoardTo(1f, timing.boardFadeDuration));
            }

            if (step.IsReaction)
                AppendReaction(transition, change, timing);
            else
                AppendPlayableEntrance(transition, change, timing);
        }

        /// <summary>Wraps a transition callback so it only runs while its step is still current.</summary>
        private TweenCallback ForStep(TutorialStepChange owner, Action action)
        {
            return () =>
            {
                if (_current == owner)
                    action();
            };
        }

        private void AppendExit(Sequence transition, TutorialStep step, TutorialTimingAssetData timing)
        {
            float exitAt = transition.Duration();
            if (_view != null)
            {
                transition.Insert(exitAt, _view.StopPointer(timing.captionFadeOutDuration));
                transition.Insert(exitAt, _view.CaptionsOut(timing.captionFadeOutDuration));
            }
            else
            {
                transition.AppendInterval(timing.captionFadeOutDuration);
            }

            float boardAlpha = step.IsReaction ? ReactionAlpha(timing) : 1f;
            if (!Mathf.Approximately(_board.BoardAlpha, boardAlpha))
                transition.Insert(exitAt, _board.FadeBoardTo(boardAlpha, timing.boardFadeDuration));
        }

        private void AppendReaction(Sequence transition, TutorialStepChange change, TutorialTimingAssetData timing)
        {
            TutorialStep step = change.Step;
            Tween caption = _view != null
                ? _view.CaptionIn(CoachCaptionSlot.Center, step.CaptionCenter, timing.captionFadeInDuration)
                : null;
            if (caption != null)
                transition.Append(caption);

            transition.AppendInterval(timing.reactionMinDisplay);
            transition.AppendCallback(ForStep(change, ArmReactionContinue));

            float autoAdvance = timing.reactionAutoAdvance;
            if (_view == null || !_view.HasContinueBlocker)
                autoAdvance = Mathf.Max(autoAdvance, 1.5f);
            if (autoAdvance > 0f)
            {
                transition.AppendInterval(autoAdvance);
                transition.AppendCallback(ForStep(change, _sequencer.Continue));
            }
        }

        private void AppendPlayableEntrance(
            Sequence transition,
            TutorialStepChange change,
            TutorialTimingAssetData timing)
        {
            TutorialStep step = change.Step;
            float start = transition.Duration();

            Sequence reveal = change.RevealedBlobIds.Count > 0 || change.RevealedCells.Count > 0
                ? _board.CreateRevealSequence(
                    change.RevealedCells,
                    change.RevealedBlobIds,
                    BoardRevealOptions.From(timing))
                : null;
            Sequence captions = CreateCaptionSequence(step, timing);

            float revealLength = reveal != null ? reveal.Duration() : 0f;
            float captionLength = captions != null ? captions.Duration() : 0f;
            float revealAt = start;
            float captionAt = start;
            switch (timing.entranceOrder)
            {
                case TutorialEntranceOrder.RevealThenCaptions:
                    if (reveal != null)
                        captionAt += revealLength + timing.entranceGap;
                    break;
                case TutorialEntranceOrder.CaptionsThenReveal:
                    if (captions != null)
                        revealAt += captionLength + timing.entranceGap;
                    break;
            }

            if (reveal != null)
                transition.Insert(revealAt, reveal);
            if (captions != null)
                transition.Insert(captionAt, captions);

            transition.AppendCallback(ForStep(change, () => _board.SetInteractionLocked(false)));

            if (step.ShowPointer && _view != null)
            {
                transition.AppendInterval(timing.pointerDelay);
                transition.AppendCallback(ForStep(change, PlayPointer));
            }
        }

        private Sequence CreateCaptionSequence(TutorialStep step, TutorialTimingAssetData timing)
        {
            if (_view == null)
                return null;

            Tween top = _view.CaptionIn(CoachCaptionSlot.Top, step.CaptionTop, timing.captionFadeInDuration);
            Tween bottom = _view.CaptionIn(CoachCaptionSlot.Bottom, step.CaptionBottom, timing.captionFadeInDuration);
            if (top == null && bottom == null)
                return null;

            Tween first = timing.captionOrder == TutorialCaptionOrder.BottomFirst ? bottom : top;
            Tween second = timing.captionOrder == TutorialCaptionOrder.BottomFirst ? top : bottom;
            float secondAt = timing.captionOrder == TutorialCaptionOrder.Together || first == null
                ? 0f
                : timing.captionStagger;

            Sequence captions = DOTween.Sequence();
            if (first != null)
                captions.Insert(0f, first);
            if (second != null)
                captions.Insert(secondAt, second);
            return captions;
        }

        /// <summary>
        /// A player Restart rebuilds the board mid-transition; jump to the step's settled look.
        /// </summary>
        private void HandleStateRestored(GameSessionSnapshot snapshot)
        {
            if (_current == null || _sequencer.IsCompleted)
                return;

            KillTransition(complete: false);
            TutorialStep step = _current.Step;
            TutorialTimingAssetData timing = _asset.GetTiming(_current.Index);

            _board.SetBoardAlpha(step.IsReaction ? ReactionAlpha(timing) : 1f);
            if (step.IsReaction)
            {
                _board.SetInteractionLocked(true);
                if (_view != null)
                    _view.ShowCaptions(top: null, bottom: null, center: step.CaptionCenter);
                ArmReactionContinue();
                return;
            }

            _board.SetInteractionLocked(false);
            if (_view == null)
                return;

            _view.ShowCaptions(step.CaptionTop, step.CaptionBottom, center: null);
            if (step.ShowPointer)
                PlayPointer();
        }

        /// <summary>
        /// Raised when the closing fade after the last step finishes and the board is fully faded out.
        /// The host loads whatever comes next from here.
        /// </summary>
        public event Action OutroCompleted;

        /// <summary>Board fade length of the closing outro, for the next session's fade-in.</summary>
        public float OutroFadeDuration { get; private set; }

        private void HandleSequenceCompleted()
        {
            // Cleared first so the last step's callbacks see they are stale while it completes.
            _current = null;
            KillTransition(complete: true);
            if (_view != null)
                _view.DisarmContinue();
            _board.SetInteractionLocked(true);

            TutorialTimingAssetData timing = _asset.GetTiming(_sequencer.StepCount - 1);
            OutroFadeDuration = timing.boardFadeDuration;

            Sequence outro = DOTween.Sequence().SetLink(_board.gameObject, LinkBehaviour.KillOnDestroy);
            _transition = outro;
            outro.AppendInterval(timing.holdBeforeTransition);
            float fadeAt = outro.Duration();
            if (_view != null)
            {
                outro.Insert(fadeAt, _view.StopPointer(timing.captionFadeOutDuration));
                outro.Insert(fadeAt, _view.CaptionsOut(timing.captionFadeOutDuration));
            }
            outro.Insert(fadeAt, _board.FadeBoardTo(0f, timing.boardFadeDuration));
            outro.AppendInterval(timing.gapAfterFadeOut);
            outro.AppendCallback(() =>
            {
                if (_transition == outro)
                    OutroCompleted?.Invoke();
            });
        }

        private void ArmReactionContinue()
        {
            if (_view != null && _view.HasContinueBlocker)
                _view.ArmContinue(_sequencer.Continue, armDelay: 0f);
        }

        private void PlayPointer()
        {
            if (_view == null || _current == null)
                return;

            _view.Play(
                GridToLocal(_current.Step.CoachFrom),
                GridToLocal(_current.Step.CoachTo),
                caption: null);
        }

        private void ResetBoard()
        {
            _board.SetBoardAlpha(1f);
            _board.SetInteractionLocked(false);
        }

        private void KillTransition(bool complete)
        {
            Sequence transition = _transition;
            _transition = null;
            if (transition != null && transition.IsActive())
                transition.Kill(complete);
        }

        private static float ReactionAlpha(TutorialTimingAssetData timing)
        {
            return timing.reactionBoard switch
            {
                TutorialReactionBoard.Hide => 0f,
                TutorialReactionBoard.Keep => 1f,
                _ => timing.reactionDimAlpha
            };
        }

        private Vector3 GridToLocal(GridPosition position)
        {
            return new Vector3(
                _board.Origin.x + position.X * _board.CellSize,
                _board.Origin.y + position.Y * _board.CellSize,
                0f);
        }
    }
}
