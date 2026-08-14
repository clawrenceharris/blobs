namespace Blobs.Core
{
    /// <summary>
    /// Represents one deterministic mutation that can be applied to a <see cref="BoardState"/>.
    /// Effects are emitted by Core resolution and consumed by Presentation for visual feedback.
    /// </summary>
    public interface IBoardEffect
    {
        /// <summary>
        /// Applies this already-resolved effect to the supplied board. Implementations should not
        /// perform gameplay validation; validation belongs in the resolver before effects are emitted.
        /// </summary>
        void Apply(BoardState board);
    }
}
