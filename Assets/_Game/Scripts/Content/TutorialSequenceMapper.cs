using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;

namespace Blobs.Content
{
    /// <summary>
    /// Converts an authored <see cref="TutorialSequenceAsset"/> into a Unity-free
    /// <see cref="TutorialDefinition"/>. Steps map one-to-one, so asset step indices match
    /// sequencer step indices.
    /// </summary>
    /// <remarks>
    /// A cell is hidden at the start when it is marked empty on the board asset or listed in a later
    /// step's reveal cells. Revealing a blob that sits on an authored empty cell also opens that cell.
    /// Board blobs on a hidden cell enter with that cell unless a step reveals them explicitly.
    /// Empty cells that no step reveals stay empty for the whole tutorial.
    /// </remarks>
    public static class TutorialSequenceMapper
    {
        public static TutorialDefinition ToApplication(TutorialSequenceAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (asset.Board == null)
                throw new InvalidOperationException($"Tutorial '{asset.name}' has no board.");
            if (asset.Steps.Count == 0)
                throw new InvalidOperationException($"Tutorial '{asset.name}' has no steps.");

            LevelDefinition board = LevelAssetMapper.ToCore(asset.Board);
            var blobsById = new Dictionary<string, BlobDefinition>();
            foreach (BlobDefinition blob in board.Blobs)
                blobsById[blob.Id] = blob;

            var authoredEmpty = new HashSet<GridPosition>(board.EmptyPositions);
            List<BlobDefinition> staged = CollectStagedPieces(asset, board, blobsById);
            var stagedIds = new HashSet<string>();
            foreach (BlobDefinition blob in staged)
                stagedIds.Add(blob.Id);

            for (int i = 0; i < asset.Steps.Count; i++)
            {
                if (asset.Steps[i] == null)
                    throw new InvalidOperationException($"Tutorial '{asset.name}' step {i} is empty.");
            }

            Dictionary<GridPosition, int> revealedCellStep = CollectRevealedCells(asset, board);
            Dictionary<string, int> explicitBlobStep = CollectExplicitBlobReveals(asset, blobsById);
            // Listing nest/red on a step is enough: their authored-empty cells open with them.
            InferCellsFromBlobReveals(asset, blobsById, authoredEmpty, explicitBlobStep, revealedCellStep);

            // Board blobs on a hidden cell ride in with the cell unless a step reveals them itself.
            var autoRevealsByStep = new Dictionary<int, List<BlobDefinition>>();
            var revealedBlobStep = new Dictionary<string, int>(explicitBlobStep);
            foreach (BlobDefinition blob in board.Blobs)
            {
                if (explicitBlobStep.ContainsKey(blob.Id) ||
                    !revealedCellStep.TryGetValue(blob.Position, out int cellStep) ||
                    cellStep == 0)
                    continue;

                revealedBlobStep[blob.Id] = cellStep;
                if (!autoRevealsByStep.TryGetValue(cellStep, out List<BlobDefinition> list))
                    autoRevealsByStep[cellStep] = list = new List<BlobDefinition>();
                list.Add(blob);
            }

            ValidatePlacement(asset, board, blobsById.Values, stagedIds, authoredEmpty, revealedCellStep, revealedBlobStep);

            var removedBlobStep = new Dictionary<string, int>();
            var steps = new List<TutorialStep>(asset.Steps.Count);
            for (int i = 0; i < asset.Steps.Count; i++)
            {
                TutorialStepAssetData data = asset.Steps[i];

                if (data.kind == TutorialStepKind.Reaction)
                {
                    if (string.IsNullOrWhiteSpace(data.captionCenter) &&
                        string.IsNullOrWhiteSpace(data.captionTop) &&
                        string.IsNullOrWhiteSpace(data.captionBottom))
                        throw Error(asset, i, "reaction needs at least one caption (top, bottom, or center).");
                    if (Count(data.revealBlobIds) > 0 || Count(data.revealCells) > 0 || Count(data.removeBlobIds) > 0)
                        throw Error(asset, i, "reaction slides cannot change the board; move the change to the next playable step.");

                    steps.Add(TutorialStep.Reaction(
                        data.captionTop,
                        data.captionBottom,
                        data.captionCenter,
                        data.reactionBoard));
                    continue;
                }

                var removals = new List<string>();
                if (data.removeBlobIds != null)
                {
                    foreach (string id in data.removeBlobIds)
                    {
                        if (i == 0)
                            throw Error(asset, i, "the first step cannot remove blobs; leave them off the board instead.");
                        if (string.IsNullOrWhiteSpace(id) || !blobsById.ContainsKey(id))
                            throw Error(asset, i, $"remove blob '{id}' is not on the board or staged pieces.");
                        if (revealedBlobStep.TryGetValue(id, out int shownAt) ? shownAt >= i : stagedIds.Contains(id))
                            throw Error(asset, i, $"blob '{id}' is removed before it is revealed.");
                        if (!removedBlobStep.TryAdd(id, i))
                            throw Error(asset, i, $"blob '{id}' is already removed by step {removedBlobStep[id]}.");
                        removals.Add(id);
                    }
                }

                var cells = new List<GridPosition>();
                var cellSet = new HashSet<GridPosition>();
                if (data.revealCells != null)
                {
                    foreach (var c in data.revealCells)
                    {
                        var cell = new GridPosition(c.x, c.y);
                        if (cellSet.Add(cell))
                            cells.Add(cell);
                    }
                }
                foreach (KeyValuePair<GridPosition, int> pair in revealedCellStep)
                {
                    if (pair.Value == i && cellSet.Add(pair.Key))
                        cells.Add(pair.Key);
                }

                var blobs = new List<BlobDefinition>();
                if (data.revealBlobIds != null)
                {
                    foreach (string id in data.revealBlobIds)
                        blobs.Add(blobsById[id]);
                }
                if (autoRevealsByStep.TryGetValue(i, out List<BlobDefinition> auto))
                    blobs.AddRange(auto);

                var allowedSources = new List<string>();
                if (data.allowedSourceBlobIds != null)
                {
                    foreach (string id in data.allowedSourceBlobIds)
                    {
                        if (string.IsNullOrWhiteSpace(id) || !blobsById.ContainsKey(id))
                            throw Error(asset, i, $"allowed source '{id}' is not on the board or staged pieces.");
                        allowedSources.Add(id);
                    }
                }

                steps.Add(TutorialStep.Playable(
                    data.captionTop,
                    data.advanceWhen,
                    data.captionBottom,
                    data.captionCenter,
                    blobs,
                    cells,
                    data.showPointer,
                    new GridPosition(data.coachFrom.x, data.coachFrom.y),
                    new GridPosition(data.coachTo.x, data.coachTo.y),
                    data.failureFilter,
                    removals,
                    allowedSources));
            }

            if (steps[0].IsReaction)
                throw new InvalidOperationException(
                    $"Tutorial '{asset.name}' must start with a playable step so the board can load.");

            foreach (KeyValuePair<string, int> removed in removedBlobStep)
            {
                if (revealedBlobStep.TryGetValue(removed.Key, out int shownAt) && shownAt > removed.Value)
                    throw Error(asset, shownAt,
                        $"blob '{removed.Key}' was removed by step {removed.Value} and cannot return.");
            }

            return new TutorialDefinition(
                CreateInitialLevel(asset, board, staged, authoredEmpty, revealedBlobStep, revealedCellStep),
                steps);
        }

        private static List<BlobDefinition> CollectStagedPieces(
            TutorialSequenceAsset asset,
            LevelDefinition board,
            Dictionary<string, BlobDefinition> blobsById)
        {
            var staged = new List<BlobDefinition>();
            foreach (LevelDefinitionAsset holder in asset.StagedPieces)
            {
                if (holder == null)
                    continue;

                foreach (BlobDefinition blob in LevelAssetMapper.ToCoreBlobs(holder))
                {
                    if (!blobsById.TryAdd(blob.Id, blob))
                        throw new InvalidOperationException(
                            $"Tutorial '{asset.name}': staged blob id '{blob.Id}' in '{holder.name}' is already used.");
                    if (!IsInside(board, blob.Position))
                        throw new InvalidOperationException(
                            $"Tutorial '{asset.name}': staged blob '{blob.Id}' at {blob.Position} is outside the board.");
                    staged.Add(blob);
                }
            }

            return staged;
        }

        private static Dictionary<GridPosition, int> CollectRevealedCells(TutorialSequenceAsset asset, LevelDefinition board)
        {
            var revealedCellStep = new Dictionary<GridPosition, int>();
            for (int i = 0; i < asset.Steps.Count; i++)
            {
                TutorialStepAssetData data = asset.Steps[i];
                if (data.kind == TutorialStepKind.Reaction || data.revealCells == null)
                    continue;

                foreach (var c in data.revealCells)
                {
                    var cell = new GridPosition(c.x, c.y);
                    if (!IsInside(board, cell))
                        throw Error(asset, i, $"reveal cell {cell} is outside the board.");
                    if (!revealedCellStep.TryAdd(cell, i))
                        throw Error(asset, i, $"cell {cell} is already revealed by step {revealedCellStep[cell]}.");
                }
            }

            return revealedCellStep;
        }

        private static Dictionary<string, int> CollectExplicitBlobReveals(
            TutorialSequenceAsset asset,
            Dictionary<string, BlobDefinition> blobsById)
        {
            var revealedBlobStep = new Dictionary<string, int>();
            for (int i = 0; i < asset.Steps.Count; i++)
            {
                TutorialStepAssetData data = asset.Steps[i];
                if (data.kind == TutorialStepKind.Reaction || data.revealBlobIds == null)
                    continue;

                foreach (string id in data.revealBlobIds)
                {
                    if (string.IsNullOrWhiteSpace(id) || !blobsById.ContainsKey(id))
                        throw Error(asset, i, $"reveal blob '{id}' is not on the board or staged pieces.");
                    if (!revealedBlobStep.TryAdd(id, i))
                        throw Error(asset, i, $"blob '{id}' is already revealed by step {revealedBlobStep[id]}.");
                }
            }

            return revealedBlobStep;
        }

        /// <summary>
        /// When a step reveals a blob that sits on an authored empty cell, that cell opens on the
        /// same step. Authors only need the blob id; listing the cell is optional.
        /// </summary>
        private static void InferCellsFromBlobReveals(
            TutorialSequenceAsset asset,
            Dictionary<string, BlobDefinition> blobsById,
            HashSet<GridPosition> authoredEmpty,
            Dictionary<string, int> explicitBlobStep,
            Dictionary<GridPosition, int> revealedCellStep)
        {
            foreach (KeyValuePair<string, int> pair in explicitBlobStep)
            {
                BlobDefinition blob = blobsById[pair.Key];
                if (!authoredEmpty.Contains(blob.Position))
                    continue;

                if (!revealedCellStep.TryAdd(blob.Position, pair.Value))
                {
                    int cellStep = revealedCellStep[blob.Position];
                    if (pair.Value < cellStep)
                        throw Error(asset, pair.Value,
                            $"blob '{blob.Id}' is revealed at step {pair.Value} but cell {blob.Position} " +
                            $"only opens at step {cellStep}.");
                }
            }
        }

        /// <summary>
        /// Every piece must land on a cell that is open by the time it appears.
        /// </summary>
        private static void ValidatePlacement(
            TutorialSequenceAsset asset,
            LevelDefinition board,
            IEnumerable<BlobDefinition> blobs,
            HashSet<string> stagedIds,
            HashSet<GridPosition> authoredEmpty,
            Dictionary<GridPosition, int> revealedCellStep,
            Dictionary<string, int> revealedBlobStep)
        {
            foreach (BlobDefinition blob in blobs)
            {
                bool revealed = revealedBlobStep.TryGetValue(blob.Id, out int blobStep);
                if (!revealed && stagedIds.Contains(blob.Id))
                    throw new InvalidOperationException(
                        $"Tutorial '{asset.name}': staged blob '{blob.Id}' is never revealed by a step.");

                if (revealedCellStep.TryGetValue(blob.Position, out int cellStep))
                {
                    if (blobStep < cellStep)
                        throw new InvalidOperationException(
                            $"Tutorial '{asset.name}': blob '{blob.Id}' sits on cell {blob.Position}, " +
                            $"which opens at step {cellStep}. Reveal the blob at or after that step.");
                }
                else if (authoredEmpty.Contains(blob.Position))
                {
                    throw new InvalidOperationException(
                        $"Tutorial '{asset.name}': blob '{blob.Id}' sits on empty cell {blob.Position}, " +
                        "which no step reveals. Add the cell to a step's reveal cells.");
                }
            }

            foreach (TileDefinition tile in board.Tiles)
            {
                bool hiddenLater = revealedCellStep.TryGetValue(tile.Position, out int cellStep)
                    ? cellStep > 0
                    : authoredEmpty.Contains(tile.Position);
                if (hiddenLater)
                    throw new InvalidOperationException(
                        $"Tutorial '{asset.name}': tile '{tile.Id}' sits on hidden cell {tile.Position}. " +
                        "Tiles must be on cells that are open from the start.");
            }
        }

        /// <summary>
        /// The opening board: pieces not revealed after step 0, with every cell that opens later
        /// marked empty. Step 0's reveals are present from the start and only animate in.
        /// </summary>
        private static LevelDefinition CreateInitialLevel(
            TutorialSequenceAsset asset,
            LevelDefinition board,
            IReadOnlyList<BlobDefinition> staged,
            HashSet<GridPosition> authoredEmpty,
            Dictionary<string, int> revealedBlobStep,
            Dictionary<GridPosition, int> revealedCellStep)
        {
            var blobs = new List<BlobDefinition>(board.Blobs.Count);
            var occupied = new Dictionary<GridPosition, string>();
            foreach (BlobDefinition blob in board.Blobs)
            {
                if (!revealedBlobStep.TryGetValue(blob.Id, out int step) || step == 0)
                {
                    blobs.Add(blob);
                    occupied[blob.Position] = blob.Id;
                }
            }

            foreach (BlobDefinition blob in staged)
            {
                if (revealedBlobStep[blob.Id] != 0)
                    continue;
                if (occupied.TryGetValue(blob.Position, out string other))
                    throw new InvalidOperationException(
                        $"Tutorial '{asset.name}': staged blob '{blob.Id}' is revealed at step 0 on " +
                        $"{blob.Position}, which '{other}' already occupies.");
                blobs.Add(blob);
                occupied[blob.Position] = blob.Id;
            }

            var empty = new HashSet<GridPosition>(authoredEmpty);
            foreach (KeyValuePair<GridPosition, int> cell in revealedCellStep)
            {
                if (cell.Value > 0)
                    empty.Add(cell.Key);
                else
                    empty.Remove(cell.Key);
            }

            return new LevelDefinition(
                board.Id,
                board.SchemaVersion,
                board.Width,
                board.Height,
                blobs,
                board.Tiles,
                new List<GridPosition>(empty),
                board.Objective);
        }

        private static bool IsInside(LevelDefinition board, GridPosition p) =>
            p.X >= 0 && p.Y >= 0 && p.X < board.Width && p.Y < board.Height;

        private static int Count<T>(ICollection<T> list) => list?.Count ?? 0;

        private static InvalidOperationException Error(TutorialSequenceAsset asset, int step, string message)
        {
            return new InvalidOperationException($"Tutorial '{asset.name}' step {step}: {message}");
        }
    }
}
