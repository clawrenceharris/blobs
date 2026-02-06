using System.Collections.Generic;
using UnityEngine;

namespace Blobs.Core.Merge
{
    public interface IAction
    {
        void Execute();
        void Undo();
    }
    /// <summary>
    /// Command that executes a MergePlan (new event-based shape) on the board model.
    /// Execute: runs Plan.Events then Plan.DeferredEvents.
    /// Undo: runs Plan.DeferredEvents reversed then Plan.Events reversed.
    /// </summary>
    public class MergeAction : IAction
    {
        private readonly MergePlan _plan;
        private readonly IBoardPresenter _board;

        public MergePlan Plan => _plan;
        public Blob Blob { get; set; }

        public MergeAction(MergePlan plan, IBoardPresenter board)
        {
            _plan = plan;
            _board = board;
        }

        public void Execute()
        {
            foreach (var e in _plan.Events)
                e.Execute(_board);
            foreach (var e in _plan.DeferredEvents)
                e.Execute(_board);
        }

        public void Undo()
        {
            for (int i = _plan.DeferredEvents.Count - 1; i >= 0; i--)
                _plan.DeferredEvents[i].Undo(_board);
            for (int i = _plan.Events.Count - 1; i >= 0; i--)
                _plan.Events[i].Undo(_board);
        }
    }
}
