using System;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns the drag-preview subscription and maps the session's drag source, tail cell, and verdict
    /// onto the board-space swipe indicator. Blob selection animation stays with <see cref="BlobSelectionPresenter"/>.
    /// </summary>
    internal sealed class SwipeIndicatorPresenter : IDisposable
    {
        private readonly SwipeIndicatorView _view;
        private readonly IGameplayState _state;
        private readonly LevelColorPaletteAsset _palette;
        private readonly float _cellSize;
        private readonly Vector2 _origin;
        private string _presentedSourceId;

        public SwipeIndicatorPresenter(
            SwipeIndicatorView view,
            IGameplayState state,
            LevelColorPaletteAsset palette,
            float cellSize,
            Vector2 origin)
        {
            _view = view != null ? view : throw new ArgumentNullException(nameof(view));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _palette = palette != null ? palette : throw new ArgumentNullException(nameof(palette));
            _cellSize = cellSize;
            _origin = origin;

            _view.Configure(cellSize);
            _view.HideImmediately();
            _state.DragPreviewChanged += Present;
        }

        public void Present(DragPreview preview)
        {
            if (preview == null || !preview.IsActive)
            {
                if (_presentedSourceId != null)
                    _view.Hide();
                _presentedSourceId = null;
                return;
            }

            Color tint = preview.Verdict == DragVerdict.Invalid
                ? _palette.InvalidMoveColor
                : _palette.ValidMoveColor;

            if (_presentedSourceId != preview.SourceId)
            {
                _view.Show(GridToLocal(preview.Origin), tint);
                _presentedSourceId = preview.SourceId;
            }
            else
            {
                _view.SetTint(tint);
            }

            GridPosition offset = preview.Tail - preview.Origin;
            _view.ExtendTo(new Vector2Int(offset.X, offset.Y));
        }

        public void Dispose()
        {
            _state.DragPreviewChanged -= Present;
            _presentedSourceId = null;
            if (_view != null)
                _view.HideImmediately();
        }

        private Vector3 GridToLocal(GridPosition position)
        {
            return new Vector3(_origin.x + position.X * _cellSize, _origin.y + position.Y * _cellSize, 0f);
        }
    }
}
