namespace Blobs.Core
{
    /// <summary>
    /// Creates unique ids for blobs spawned by Core during move resolution
    /// (authored blobs get their ids from level content).
    /// </summary>
    public interface IBlobIdFactory
    {
        /// <summary>
        /// Returns an id starting with <paramref name="prefix"/> that does not
        /// collide with any blob on <paramref name="board"/>.
        /// </summary>
        string CreateId(BoardState board, string prefix);
    }

    /// <summary>
    /// Deterministic default id factory producing "{prefix}-{n}" with an
    /// incrementing counter, skipping ids already present on the board.
    /// </summary>
    public sealed class SequentialBlobIdFactory : IBlobIdFactory
    {
        private int _next = 1;

        public string CreateId(BoardState board, string prefix)
        {
            string id;
            do
            {
                id = $"{prefix}-{_next}";
                _next++;
            }
            while (board != null && board.GetBlob(id) != null);

            return id;
        }
    }
}
