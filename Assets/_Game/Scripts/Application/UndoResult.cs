using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Application
{
    public sealed class UndoResult
    {
        public UndoResult(bool succeeded, IReadOnlyList<IBoardEffect> effects, bool isComplete)
        {
            Succeeded = succeeded;
            Effects = effects ?? new List<IBoardEffect>();
            IsComplete = isComplete;
        }

        public bool Succeeded { get; }
        public IReadOnlyList<IBoardEffect> Effects { get; }
        public bool IsComplete { get; }

        public static UndoResult Failed(bool isComplete)
        {
            return new UndoResult(false, new List<IBoardEffect>(), isComplete);
        }
    }
}
