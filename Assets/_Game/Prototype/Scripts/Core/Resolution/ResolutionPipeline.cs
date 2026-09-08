using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// Runs resolution rules and reactions to produce effects. Processes effect queue and applies via transaction.
/// </summary>
public sealed class ResolutionPipeline
{
    private readonly List<IMoveRule> _rules;
    private readonly List<IReaction> _reactions;

    public ResolutionPipeline(IEnumerable<IMoveRule> rules = null, IEnumerable<IReaction> reactions = null)
    {
        _rules = rules != null ? new List<IMoveRule>(rules) : new List<IMoveRule>();
        _reactions = reactions != null ? new List<IReaction>(reactions) : new List<IReaction>();
    }

    /// <summary>
    /// Runs rules (validation + effect enqueue), then processes queue: apply each effect via txn and dispatch reactions.
    /// </summary>
    public void Resolve(MoveContext ctx, BoardTransaction txn, MoveResult result)
    {
        if (ctx == null)
        {
            result.IsValid = false;
            result.MoveFailReason = MergeFailReason.None;
            return;
        }
        var queue = new Queue<IEffect>();
        result.Effects = new List<IEffect>();

        // Run rules (validation)
        foreach (var rule in _rules)
        {
            if (!rule.Apply(ctx, queue, out var reason))
            {

                result.IsValid = false;
                result.MoveFailReason = reason;
                return;
            }
        }

        // Apply queued effects immediately (simulation step)
        Queue<IEffect> appliedQueue = new Queue<IEffect>();
        while (queue.Count > 0)
        {
            var e = queue.Dequeue();
            
            appliedQueue.Enqueue(e);
            // Debug.Log("[ResolutionPipeline] Applied effect: " + e.GetType().Name);
            // Dispatch reactions that may enqueue more effects
            foreach (var r in _reactions)
                r.OnEffectApplied(ctx, e, appliedQueue);
            Debug.Log("[ResolutionPipeline] Applied effect: " + e.GetType().Name);

        }
        while (appliedQueue.Count > 0)
        {
            var e = appliedQueue.Dequeue();
            Debug.Log("[ResolutionPipeline] Added effect: " + e.GetType().Name);
            result.Effects.Add(e);
            txn.Apply(e);
        }
        result.IsValid = true;
        result.Context = ctx;
    }

    /// <summary>
    /// Creates a pipeline with the default rules and reactions
    /// </summary>
    public static ResolutionPipeline CreateDefault()
    {
        return new ResolutionPipeline(
            rules: new IMoveRule[]
            {
                new ValidateIntentRule(),
                new ValidateTraversalRule(),
                new ValidateLaserRule(),
                new ValidateTargetRule(),
                new BuildBasicMergeRule(),

            },
            reactions: new IReaction[] { new TrailReaction()}
        );
    }
}


