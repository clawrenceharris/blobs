using Blobs.Core;

namespace Blobs.Presentation
{
    /// <summary>
    /// Implemented by tile-prefab components that bind presentation to logical tile state.
    /// Multiple bindings can be composed on a prefab without changing presenter code.
    /// </summary>
    public interface ITileStateBinding
    {
        void Bind(TilePresentationContext context);
    }

    /// <summary>
    /// State supplied to presentation-only components when a tile view is initialized.
    /// </summary>
    public readonly struct TilePresentationContext
    {
        public TilePresentationContext(TileView view, TileState state)
        {
            View = view;
            State = state;
        }

        public TileView View { get; }
        public TileState State { get; }
    }
}
