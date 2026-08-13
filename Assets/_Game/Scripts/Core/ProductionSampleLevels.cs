using System.Collections.Generic;

namespace Blobs.Core
{
    public static class ProductionSampleLevels
    {
        public static LevelDefinition CreateMilestoneOnePuzzle()
        {
            return new LevelDefinition(
                "milestone_001",
                3,
                1,
                new List<BlobState>
                {
                    new BlobState("red_a", BlobType.Normal, BlobColor.Red, BlobSize.Normal, new GridPosition(0, 0)),
                    new BlobState("red_b", BlobType.Normal, BlobColor.Red, BlobSize.Normal, new GridPosition(2, 0))
                });
        }
    }
}
