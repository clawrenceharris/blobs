using System.Collections.Generic;

namespace Blobs.Core
{
    public static class ObjectiveEvaluator
    {
        /// <summary>
        /// Levels without objective items fall back to clearing every clearable blob.
        /// </summary>
        public static bool IsComplete(BoardState board, ObjectiveProgress progress = null)
        {
            if (board == null)
                return false;

            return progress != null && progress.HasItems
                ? progress.IsComplete
                : !board.HasAnyClearableBlobs();
        }

        /// <summary>
        /// Extracts objective outcomes from effects that were already applied to
        /// <paramref name="board"/>.
        /// </summary>
        public static IReadOnlyList<ObjectiveEvent> CollectEvents(
            BoardState board,
            IReadOnlyList<IBoardEffect> effects)
        {
            var events = new List<ObjectiveEvent>();
            if (effects == null)
                return events;

            foreach (IBoardEffect effect in effects)
            {
                switch (effect)
                {
                    case MergeEffect merge:
                        if (merge.ConsumedBlob != null)
                            events.Add(ObjectiveEvent.Cleared(merge.ConsumedBlob));
                        break;
                    case MergeIntoNestEffect nest:
                        if (nest.SourceBlob != null)
                            events.Add(ObjectiveEvent.Cleared(nest.SourceBlob));
                        if (nest.NestBlob != null)
                            events.Add(ObjectiveEvent.Nested(nest.NestBlob));
                        break;
                    case RemoveBlobEffect remove when remove.Blob != null:
                        events.Add(ObjectiveEvent.Cleared(remove.Blob));
                        break;
                    case GhostHauntEffect haunt when haunt.LandingBlob != null:
                        events.Add(ObjectiveEvent.Cleared(haunt.LandingBlob));
                        break;
                    case GhostRestEffect rest:
                        if (rest.GhostBlob != null)
                            events.Add(ObjectiveEvent.Cleared(rest.GhostBlob));
                        if (rest.LandingBlob != null)
                            events.Add(ObjectiveEvent.Cleared(rest.LandingBlob));
                        TileState grave = board?.GetTileAt(rest.RestDestination);
                        if (grave != null)
                            events.Add(ObjectiveEvent.Activated(grave));
                        break;
                }
            }

            return events;
        }
    }
}
