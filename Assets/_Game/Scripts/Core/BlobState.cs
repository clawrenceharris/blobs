using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Immutable logical state for a blob in Core. Presentation should render this data but not mutate it.
    /// </summary>
    public sealed class BlobState
    {
        private Dictionary<Type, IBlobModel> models { get; } = new();

        /// <summary>
        /// Creates a blob state with a stable id and logical grid position.
        /// </summary>
        public BlobState(
            string id,
            BlobType type,
            GridPosition position
          )
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Blob id cannot be empty.", nameof(id));

            Id = id;
            Type = type;
            Position = position;
        }



        public string Id { get; }
        public BlobType Type { get; }
        public GridPosition Position { get; }



        public bool IsClearable =>
            Type == BlobType.Normal || Type == BlobType.Trail;

        /// <summary>
        /// Creates a copy of this blob at a new grid position while preserving id and traits.
        /// </summary>
        public BlobState WithPosition(GridPosition position)
        {
            return new BlobState(Id, Type, position);
        }
        public BlobState AddModel<T>(T model) where T : class, IBlobModel
        {
            models.Add(typeof(T), model);
            return this;
        }
        public T GetModel<T>() where T : class, IBlobModel
        {
            return (T)models.GetValueOrDefault(typeof(T));

        }
        public bool TryGetModel<T>(out T model) where T : class, IBlobModel
        {

            // First, try to find an exact type match
            if (models.TryGetValue(typeof(T), out IBlobModel tModel))
            // If not found, search the components for a value that is compatible (handles subtypes/interfaces)
            {
                model = tModel as T;
                return true;
            }
            else
            {
                foreach (var kvp in models)
                {
                    if (kvp.Value is T tModelValue)
                    {
                        model = tModelValue;
                        return true;
                    }
                }
            }
            model = null;
            return false;
        }
        public void RemoveModel<T>() where T : class, IBlobModel
        {
            models.Remove(typeof(T));
        }
    }
    public interface IBlobModel
    {
    }

    public sealed class ColorBlobModel : IBlobModel
    {
        public BlobColor Color { get; }

        public ColorBlobModel(BlobColor color)
        {
            Color = color;
        }
    }

    public sealed class TrailBlobModel : IBlobModel
    {
        /// <summary>
        /// Color of the normal blobs a Trail blob leaves behind
        /// </summary>
        public BlobColor TrailColor { get; }

        public TrailBlobModel(BlobColor trailColor)
        {
            TrailColor = trailColor;
        }
    }

}
