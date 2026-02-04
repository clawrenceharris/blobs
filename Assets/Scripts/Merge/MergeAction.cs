using System.Collections.Generic;
using Blobs.Commands;
using UnityEngine;

namespace Blobs.Core.Merge
{
    /// <summary>
    /// Command that executes a MergePlan (new event-based shape) on the board model.
    /// Execute: runs Plan.Events then Plan.DeferredEvents.
    /// Undo: runs Plan.DeferredEvents reversed then Plan.Events reversed.
    /// </summary>
    public class MergeAction : IAction
    {
        private readonly MergePlan _plan;
        private readonly BoardModel _board;

        public MergePlan Plan => _plan;
        public Blob Blob { get; set; }

        public MergeAction(MergePlan plan, BoardModel board)
        {
            _plan = plan;
            _board = board;
        }

        public void Execute(CommandManager context)
        {
            foreach (var e in _plan.Events)
                e.Execute(_board);
            foreach (var e in _plan.DeferredEvents)
                e.Execute(_board);
        }

        public void Undo(CommandManager context)
        {
            for (int i = _plan.DeferredEvents.Count - 1; i >= 0; i--)
                _plan.DeferredEvents[i].Undo(_board);
            for (int i = _plan.Events.Count - 1; i >= 0; i--)
                _plan.Events[i].Undo(_board);
        }
    }
}
