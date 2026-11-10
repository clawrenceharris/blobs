using System;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Level-opt-in swipe coach. Keeps the looping ghost path visible through selection and
    /// aiming so the player can re-read the hint before committing. Dismisses after the first
    /// successful move.
    /// </summary>
    internal sealed class CoachPresenter : IDisposable
    {
        private readonly CoachPointerView _view;
        private readonly IGameplayState _state;
        private readonly float _cellSize;
        private readonly Vector2 _origin;
        private bool _active;

        public CoachPresenter(
            CoachPointerView view,
            IGameplayState state,
            LevelDefinitionAsset level,
            float cellSize,
            Vector2 origin)
        {
            _view = view != null ? view : throw new ArgumentNullException(nameof(view));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _cellSize = cellSize;
            _origin = origin;

            _view.HideImmediately();
            if (level == null || !level.ShowSwipeCoach)
                return;

            _active = true;
            if (level.ShowPointer)
            {
                // Play kills caption tweens, so the caption must start after the pointer loop.
                _view.Play(
                    GridToLocal(new GridPosition(level.CoachFrom.x, level.CoachFrom.y)),
                    GridToLocal(new GridPosition(level.CoachTo.x, level.CoachTo.y)),
                    level.CoachCaption);
                _state.MoveResolved += HandleMoveResolved;
            }

            _view.ShowCaption(level.CoachCaption);
        }

        public void Dispose()
        {
            if (!_active)
            {
                _view?.HideImmediately();
                return;
            }

            _state.MoveResolved -= HandleMoveResolved;
            _active = false;
            _view.Hide();
        }

        private void HandleMoveResolved(MoveResult result)
        {
            if (result != null && result.Succeeded)
                Dismiss();
        }

        private void Dismiss()
        {
            if (!_active)
                return;

            _state.MoveResolved -= HandleMoveResolved;
            _active = false;
            _view.Hide();
        }

        private Vector3 GridToLocal(GridPosition position)
        {
            return new Vector3(_origin.x + position.X * _cellSize, _origin.y + position.Y * _cellSize, 0f);
        }
    }
}
