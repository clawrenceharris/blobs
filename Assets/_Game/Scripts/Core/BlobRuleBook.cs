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
        bool TryGetMoveStrategy(
            BlobType sourceType,
            BlobType targetType,
            out IMoveStrategy moveStrategy);

    }

    public sealed class BlobRuleBook : IBlobRuleBook
    {
        private readonly Dictionary<BlobType, BlobTraits> _traits;
        private readonly Dictionary<MergeKey, IMergeStrategy> _mergeStrategies;
        private readonly Dictionary<BlobType, IMoveBehavior> _moveBehaviors;
        private readonly Dictionary<MergeKey, IMoveStrategy> _moveStrategies;

        public BlobRuleBook(
            IDictionary<BlobType, BlobTraits> traits,
            IDictionary<MergeKey, IMergeStrategy> mergeStrategies,
            IDictionary<MergeKey, IMoveStrategy> moveStrategies = null,
            IDictionary<BlobType, IMoveBehavior> moveBehaviors = null
           )
        {
            _traits = new Dictionary<BlobType, BlobTraits>(traits);
            _mergeStrategies =
                new Dictionary<MergeKey, IMergeStrategy>(mergeStrategies);
            _moveBehaviors = moveBehaviors != null
                ? new Dictionary<BlobType, IMoveBehavior>(moveBehaviors)
                : new Dictionary<BlobType, IMoveBehavior>();
            _moveStrategies = moveStrategies != null
                ? new Dictionary<MergeKey, IMoveStrategy>(moveStrategies)
                : new Dictionary<MergeKey, IMoveStrategy>();
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
        public bool TryGetMoveStrategy(
            BlobType sourceType,
            BlobType targetType,
            out IMoveStrategy strategy)
        {
            return _moveStrategies.TryGetValue(new MergeKey(sourceType, targetType), out strategy);
        }

        public bool TryGetMergeStrategy(
            BlobType sourceType,
            BlobType targetType,
            out IMergeStrategy strategy)
        {
            return _mergeStrategies.TryGetValue(
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
            var rockMerge = new RockMergeStrategy();
            var ghostMerge = new GhostMergeStrategy();

            return new BlobRuleBook(
                new Dictionary<BlobType, BlobTraits>
                {
                    [BlobType.Normal] =
                        new BlobTraits(canBeSource: true, isClearable: true),

                    [BlobType.Flag] =
                        new BlobTraits(canBeSource: false, isClearable: false),
                    [BlobType.Trail] =
                        new BlobTraits(canBeSource: true, isClearable: true),

                    [BlobType.Rock] =
                        new BlobTraits(canBeSource: false, isClearable: false),
                    [BlobType.Ghost] =
                        new BlobTraits(canBeSource: false, isClearable: false),
                },
                new Dictionary<MergeKey, IMergeStrategy>
                {
                    // Normal blobs
                    [new MergeKey(BlobType.Normal, BlobType.Normal)] =
                        normalMerge,
                    [new MergeKey(BlobType.Trail, BlobType.Normal)] =
                        normalMerge,

                    // Trail blobs
                    [new MergeKey(BlobType.Trail, BlobType.Trail)] =
                        normalMerge,
                    [new MergeKey(BlobType.Normal, BlobType.Trail)] =
                        normalMerge,

                    // Flag blobs
                    [new MergeKey(BlobType.Normal, BlobType.Flag)] =
                        flagMerge,
                    [new MergeKey(BlobType.Trail, BlobType.Flag)] =
                        flagMerge,

                    // Rock blobs
                    [new MergeKey(BlobType.Normal, BlobType.Rock)] =
                        rockMerge,
                    [new MergeKey(BlobType.Trail, BlobType.Rock)] =
                        rockMerge,

                    // Ghost blobs
                    [new MergeKey(BlobType.Normal, BlobType.Ghost)] =
                        ghostMerge,
                    [new MergeKey(BlobType.Trail, BlobType.Ghost)] =
                        ghostMerge,



                },
                 new Dictionary<MergeKey, IMoveStrategy>
                 {
                     // Normal blobs
                     [new MergeKey(BlobType.Normal, BlobType.Normal)] = new NormalMoveStrategy(),
                     [new MergeKey(BlobType.Trail, BlobType.Normal)] = new NormalMoveStrategy(),
                     // Rock blobs
                     [new MergeKey(BlobType.Normal, BlobType.Rock)] = new RockMoveStrategy(),
                     [new MergeKey(BlobType.Trail, BlobType.Rock)] = new RockMoveStrategy(),

                     // Flag blobs
                     [new MergeKey(BlobType.Normal, BlobType.Flag)] = new NormalMoveStrategy(),
                     [new MergeKey(BlobType.Trail, BlobType.Flag)] = new NormalMoveStrategy(),

                     // Ghost blobs
                     [new MergeKey(BlobType.Normal, BlobType.Ghost)] = new NormalMoveStrategy(),
                     [new MergeKey(BlobType.Trail, BlobType.Ghost)] = new NormalMoveStrategy(),
                 },


                 new Dictionary<BlobType, IMoveBehavior>
                 {
                     [BlobType.Trail] = new TrailMoveBehavior(),
                 }

               );
        }
    }
}