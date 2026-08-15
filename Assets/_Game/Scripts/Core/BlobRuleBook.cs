using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    public readonly struct MergeKey : IEquatable<MergeKey>
    {
        public MergeKey(BlobType sourceType, BlobType targetType)
        {
            SourceType = sourceType;
            TargetType = targetType;
        }

        public BlobType SourceType { get; }
        public BlobType TargetType { get; }

        public bool Equals(MergeKey other)
        {
            return SourceType == other.SourceType &&
                   TargetType == other.TargetType;
        }

        public override bool Equals(object obj)
        {
            return obj is MergeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)SourceType * 397) ^ (int)TargetType;
            }
        }
    }

    public interface IBlobRuleBook
    {
        BlobTraits GetTraits(BlobType type);

        bool TryGetMergeStrategy(
            BlobType sourceType,
            BlobType targetType,
            out IMergeStrategy strategy);

        bool TryGetMoveBehavior(
            BlobType type,
            out IMoveBehavior behavior);
    }

    public sealed class BlobRuleBook : IBlobRuleBook
    {
        private readonly Dictionary<BlobType, BlobTraits> _traits;
        private readonly Dictionary<MergeKey, IMergeStrategy> _strategies;
        private readonly Dictionary<BlobType, IMoveBehavior> _moveBehaviors;

        public BlobRuleBook(
            IDictionary<BlobType, BlobTraits> traits,
            IDictionary<MergeKey, IMergeStrategy> strategies,
            IDictionary<BlobType, IMoveBehavior> moveBehaviors = null)
        {
            _traits = new Dictionary<BlobType, BlobTraits>(traits);
            _strategies =
                new Dictionary<MergeKey, IMergeStrategy>(strategies);
            _moveBehaviors = moveBehaviors != null
                ? new Dictionary<BlobType, IMoveBehavior>(moveBehaviors)
                : new Dictionary<BlobType, IMoveBehavior>();
        }

        public BlobTraits GetTraits(BlobType type)
        {
            if (!_traits.TryGetValue(type, out BlobTraits traits))
            {
                throw new InvalidOperationException(
                    $"No traits registered for blob type {type}.");
            }

            return traits;
        }

        public bool TryGetMergeStrategy(
            BlobType sourceType,
            BlobType targetType,
            out IMergeStrategy strategy)
        {
            return _strategies.TryGetValue(
                new MergeKey(sourceType, targetType),
                out strategy);
        }

        public bool TryGetMoveBehavior(
            BlobType type,
            out IMoveBehavior behavior)
        {
            return _moveBehaviors.TryGetValue(type, out behavior);
        }

        public static BlobRuleBook CreateDefault()
        {
            var normalMerge = new NormalMergeStrategy();
            var flagMerge = new FlagMergeStrategy();

            return new BlobRuleBook(
                new Dictionary<BlobType, BlobTraits>
                {
                    [BlobType.Normal] =
                        new BlobTraits(canBeSource: true, isClearable: true),

                    [BlobType.Flag] =
                        new BlobTraits(canBeSource: false, isClearable: false),
                    [BlobType.Trail] =
                        new BlobTraits(canBeSource: true, isClearable: true)
                },
                new Dictionary<MergeKey, IMergeStrategy>
                {
                    [new MergeKey(BlobType.Normal, BlobType.Normal)] =
                        normalMerge,
                    [new MergeKey(BlobType.Trail, BlobType.Normal)] =
                        normalMerge,
                    [new MergeKey(BlobType.Normal, BlobType.Trail)] =
                        normalMerge,
                    [new MergeKey(BlobType.Trail, BlobType.Trail)] =
                        normalMerge,

                    [new MergeKey(BlobType.Normal, BlobType.Flag)] =
                        flagMerge,
                    [new MergeKey(BlobType.Trail, BlobType.Flag)] =
                        flagMerge
                },
                new Dictionary<BlobType, IMoveBehavior>
                {
                    [BlobType.Trail] = new TrailMoveBehavior()
                });
        }
    }
}