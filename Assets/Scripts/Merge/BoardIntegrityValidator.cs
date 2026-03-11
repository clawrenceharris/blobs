using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Validates board state after merge execute/undo. Use in debug or tests to ensure grid and blob registry stay consistent.
/// See Assets/Documentation/BLOB_LIFECYCLE.md.
/// </summary>
public static class BoardIntegrityValidator
{
    public struct ValidationResult
    {
        public bool Ok;
        public List<string> Errors;

        public static ValidationResult Pass() => new ValidationResult { Ok = true, Errors = new List<string>() };
        public static ValidationResult Fail(List<string> errors) => new ValidationResult { Ok = false, Errors = errors };
    }

    /// <summary>
    /// Validates that each grid cell has at most one blob, BlobCount matches grid occupancy, and every blob in the registry is on the grid at its position.
    /// </summary>
    public static ValidationResult Validate(BoardModel model)
    {
        var errors = new List<string>();
        if (model == null)
        {
            errors.Add("BoardModel is null");
            return ValidationResult.Fail(errors);
        }

        int width = model.Width;
        int height = model.Height;
        if (width <= 0 || height <= 0)
        {
            errors.Add("Invalid board dimensions");
            return ValidationResult.Fail(errors);
        }

        int gridCount = 0;
        var seenIds = new HashSet<string>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector2Int(x, y);
                var blob = model.GetBlobAt(pos);
                if (blob != null)
                {
                    gridCount++;
                    if (seenIds.Contains(blob.ID))
                        errors.Add($"Duplicate blob id {blob.ID} in grid at ({x},{y})");
                    else
                        seenIds.Add(blob.ID);

                    if (blob.GridPosition != pos)
                        errors.Add($"Blob {blob.ID} at ({x},{y}) has GridPosition {blob.GridPosition}");
                }
            }
        }

        if (gridCount != model.BlobCount)
            errors.Add($"Grid occupancy {gridCount} != BlobCount {model.BlobCount}");

        foreach (var blob in model.GetAllBlobs())
        {
            var at = model.GetBlobAt(blob.GridPosition);
            if (at != blob)
                errors.Add($"Blob {blob.ID} in registry not at grid position {blob.GridPosition}");
        }

        return errors.Count > 0 ? ValidationResult.Fail(errors) : ValidationResult.Pass();
    }

    /// <summary>
    /// Convenience: validate and log errors. Returns true if valid.
    /// </summary>
    public static bool ValidateAndLog(BoardModel model, string context = "")
    {
        var r = Validate(model);
        if (!r.Ok)
        {
            var msg = string.Join("; ", r.Errors);
            Debug.LogError($"[BoardIntegrityValidator] {context}: {msg}");
        }
        return r.Ok;
    }
}
