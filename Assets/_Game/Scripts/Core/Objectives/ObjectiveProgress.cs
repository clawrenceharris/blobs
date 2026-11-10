using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Immutable collected count for one objective item.
    /// </summary>
    public sealed class ObjectiveItemProgress
    {
        public ObjectiveItemProgress(LevelObjectiveItemDefinition item, int collected)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            Collected = collected;
        }

        public LevelObjectiveItemDefinition Item { get; }
        public ObjectiveTarget Target => Item.Target;
        public int AmountNeeded => Item.AmountNeeded;
        public int Collected { get; }
        public int Remaining => Math.Max(0, AmountNeeded - Collected);
        public bool IsComplete => Collected >= AmountNeeded;
    }

    /// <summary>
    /// Immutable progress toward every item of a level objective. Sessions keep one value per
    /// committed state so Undo and Restart restore counts with the board.
    /// </summary>
    public sealed class ObjectiveProgress
    {
        private ObjectiveProgress(IReadOnlyList<ObjectiveItemProgress> items)
        {
            Items = items;
        }

        public IReadOnlyList<ObjectiveItemProgress> Items { get; }

        public bool HasItems => Items.Count > 0;

        public bool IsComplete
        {
            get
            {
                if (!HasItems)
                    return false;

                foreach (ObjectiveItemProgress item in Items)
                {
                    if (!item.IsComplete)
                        return false;
                }

                return true;
            }
        }

        public static ObjectiveProgress Start(LevelObjectiveDefinition objective)
        {
            var items = new List<ObjectiveItemProgress>();
            if (objective != null)
            {
                foreach (LevelObjectiveItemDefinition item in objective.Items)
                    items.Add(new ObjectiveItemProgress(item, 0));
            }

            return new ObjectiveProgress(items);
        }

        /// <summary>
        /// Returns progress after counting every event against every item. One event may
        /// advance several items, such as a nested red monster counting for both a red blob
        /// item and a red nest item.
        /// </summary>
        public ObjectiveProgress Apply(IReadOnlyList<ObjectiveEvent> events)
        {
            if (events == null || events.Count == 0 || !HasItems)
                return this;

            bool changed = false;
            var items = new List<ObjectiveItemProgress>(Items.Count);
            foreach (ObjectiveItemProgress item in Items)
            {
                int matches = 0;
                foreach (ObjectiveEvent objectiveEvent in events)
                {
                    if (item.Target.Matches(objectiveEvent))
                        matches++;
                }

                if (matches == 0)
                {
                    items.Add(item);
                    continue;
                }

                changed = true;
                items.Add(new ObjectiveItemProgress(item.Item, item.Collected + matches));
            }

            return changed ? new ObjectiveProgress(items) : this;
        }
    }
}
