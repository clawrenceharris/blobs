using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Runtime references to the deterministic board component set.
    /// The default asset is generated at Resources/Board/BoardSurfaceSpriteSet.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BoardSurfaceSpriteSet",
        menuName = "Blobs/Board Surface Sprite Set")]
    public sealed class BoardSurfaceSpriteSet : ScriptableObject
    {
        [Header("Components")]
        [SerializeField] private Sprite fill;
        [SerializeField] private Sprite edgeN;
        [SerializeField] private Sprite edgeE;
        [SerializeField] private Sprite edgeS;
        [SerializeField] private Sprite edgeW;
        [SerializeField] private Sprite convexNW;
        [SerializeField] private Sprite convexNE;
        [SerializeField] private Sprite convexSW;
        [SerializeField] private Sprite convexSE;
        [SerializeField] private Sprite concaveNW;
        [SerializeField] private Sprite concaveNE;
        [SerializeField] private Sprite concaveSW;
        [SerializeField] private Sprite concaveSE;

        [Header("Shared Geometry")]
        [SerializeField, Min(1)] private int logicalCellSize = 256;
        [SerializeField, Min(1)] private int componentSize = 384;
        [SerializeField, Min(0)] private int componentPadding = 64;
        [SerializeField, Min(0)] private int seamOverlap = 2;
        [SerializeField, Min(1)] private int cornerRadius = 52;

        public Sprite Fill => fill;
        public int LogicalCellSize => logicalCellSize;
        public int ComponentSize => componentSize;
        public int ComponentPadding => componentPadding;
        public int SeamOverlap => seamOverlap;
        public int CornerRadius => cornerRadius;

        public bool IsConfigured =>
            fill != null &&
            edgeN != null && edgeE != null && edgeS != null && edgeW != null &&
            convexNW != null && convexNE != null && convexSW != null && convexSE != null &&
            concaveNW != null && concaveNE != null && concaveSW != null && concaveSE != null;

        public Sprite GetPiece(BoardSurfacePieces piece)
        {
            switch (piece)
            {
                case BoardSurfacePieces.EdgeNorth: return edgeN;
                case BoardSurfacePieces.EdgeEast: return edgeE;
                case BoardSurfacePieces.EdgeSouth: return edgeS;
                case BoardSurfacePieces.EdgeWest: return edgeW;
                case BoardSurfacePieces.ConvexNorthWest: return convexNW;
                case BoardSurfacePieces.ConvexNorthEast: return convexNE;
                case BoardSurfacePieces.ConvexSouthWest: return convexSW;
                case BoardSurfacePieces.ConvexSouthEast: return convexSE;
                case BoardSurfacePieces.ConcaveNorthWest: return concaveNW;
                case BoardSurfacePieces.ConcaveNorthEast: return concaveNE;
                case BoardSurfacePieces.ConcaveSouthWest: return concaveSW;
                case BoardSurfacePieces.ConcaveSouthEast: return concaveSE;
                default: return null;
            }
        }
    }
}
