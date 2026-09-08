using UnityEngine;

namespace Blobs.Data
{
    /// <summary>
    /// ScriptableObject that holds all 16 tile sprites for a tile type (one per neighbor configuration).
    /// Each tile variant (Normal, Laser, etc.) can have its own TileSpriteSet asset.
    /// Sprites may be null for configurations you haven't authored; fallback is isolated.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTileSpriteSet", menuName = "Blobs/Tile Sprite Set", order = 1)]
    public class TileSpriteSet : ScriptableObject
    {
        
        [Header("0–4: No / single neighbor")]
        [Tooltip("0: Isolated - no neighbors")]
        public Sprite isolated;
        [Tooltip("1: Neighbor on left only")]
        public Sprite leftOnly;
        [Tooltip("2: Neighbor on top only")]
        public Sprite topOnly;
        [Tooltip("3: Neighbor on right only")]
        public Sprite rightOnly;
        [Tooltip("4: Neighbor on bottom only")]
        public Sprite bottomOnly;

        [Header("5–8: Two neighbors (corners)")]
        [Tooltip("5: Left + top")]
        public Sprite bottomRightCorner;
        [Tooltip("6: Top + right")]
        public Sprite bottomLeftCorner;
        [Tooltip("7: Right + bottom")]
        public Sprite topLeftCorner;
        [Tooltip("8: Bottom + left")]
        public Sprite topRightCorner;

        [Header("9–10: Two neighbors (opposite sides)")]
        [Tooltip("9: Left + right (horizontal line)")]
        public Sprite horizontalLine;
        [Tooltip("10: Top + bottom (vertical line)")]
        public Sprite verticalLine;

        [Header("11–14: Three neighbors (one edge exposed)")]
        [Tooltip("11: Left + top + right (missing bottom)")]
        public Sprite missingBottom;
        [Tooltip("12: Left + top + bottom (missing right)")]
        public Sprite missingRight;
        [Tooltip("13: Left + right + bottom (missing top)")]
        public Sprite missingTop;
        [Tooltip("14: Top + right + bottom (missing left)")]
        public Sprite missingLeft;

        [Header("15: Fully surrounded")]
        [Tooltip("15: All four neighbors")]
        public Sprite fullySurrounded;

        /// <summary>
        /// Gets the sprite at the specified index (0-15). Returns isolated as fallback for null or out-of-range.
        /// </summary>
        public Sprite GetSprite(int index)
        {
            Sprite s = index switch
            {
                0 => isolated,
                1 => leftOnly,
                2 => topOnly,
                3 => rightOnly,
                4 => bottomOnly,
                5 => bottomRightCorner,
                6 => bottomLeftCorner,
                7 => topLeftCorner,
                8 => topRightCorner,
                9 => horizontalLine,
                10 => verticalLine,
                11 => missingBottom,
                12 => missingRight,
                13 => missingTop,
                14 => missingLeft,
                15 => fullySurrounded,
                _ => isolated
            };
            return s != null ? s : isolated;
        }

        /// <summary>
        /// Validates that sprites are assigned. Only logs for the original 9; newer slots are optional.
        /// </summary>
        private void OnValidate()
        {
            if (isolated == null) Debug.LogWarning($"[TileSpriteSet] {name}: Isolated (0) sprite not assigned");
            if (leftOnly == null) Debug.LogWarning($"[TileSpriteSet] {name}: LeftOnly (1) sprite not assigned");
            if (topOnly == null) Debug.LogWarning($"[TileSpriteSet] {name}: TopOnly (2) sprite not assigned");
            if (rightOnly == null) Debug.LogWarning($"[TileSpriteSet] {name}: RightOnly (3) sprite not assigned");
            if (bottomOnly == null) Debug.LogWarning($"[TileSpriteSet] {name}: BottomOnly (4) sprite not assigned");
            if (bottomRightCorner == null) Debug.LogWarning($"[TileSpriteSet] {name}: BottomRightCorner (5) sprite not assigned");
            if (bottomLeftCorner == null) Debug.LogWarning($"[TileSpriteSet] {name}: BottomLeftCorner (6) sprite not assigned");
            if (topLeftCorner == null) Debug.LogWarning($"[TileSpriteSet] {name}: TopLeftCorner (7) sprite not assigned");
            if (topRightCorner == null) Debug.LogWarning($"[TileSpriteSet] {name}: TopRightCorner (8) sprite not assigned");
        }
    }
}
