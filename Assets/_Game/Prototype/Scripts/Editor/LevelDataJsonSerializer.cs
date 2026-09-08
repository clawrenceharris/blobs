using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Blobs.Editor
{
    /// <summary>
    /// Serializes and deserializes LevelData to/from the compact JSON format used in Assets/Levels (t, c, s, tc, x, y keys).
    /// </summary>
    public static class LevelDataJsonSerializer
    {
        private static bool BlobUsesColor(BlobType type) =>
            type != BlobType.Ghost && type != BlobType.Bomb && type != BlobType.Rock;
        private static bool BlobUsesSize(BlobType type) =>
            type == BlobType.Normal || type == BlobType.Trail;
        private static bool BlobUsesTrailColor(BlobType type) => type == BlobType.Trail;
        private static bool TileUsesLaserId(TileType type) => type == TileType.Laser;
        private static bool TileUsesLaserColor(TileType type) => type == TileType.Laser;

        public static string ToJson(LevelData level)
        {
            if (level == null) return "{}";
            var root = new JObject
            {
                ["levelNum"] = level.LevelNumber,
                ["width"] = level.Width,
                ["height"] = level.Height,
                ["minMoves"] = level.MinMoves,
                ["isTutorial"] = level.IsTutorial
            };

            if (level.Scoring != null)
            {
                root["scoring"] = new JObject
                {
                    ["baseScore"] = level.Scoring.BaseScore,
                    ["movePenalty"] = level.Scoring.MovePenalty,
                    ["starThresholds"] = level.Scoring.StarThresholds != null ? JArray.FromObject(level.Scoring.StarThresholds) : new JArray(),
                    ["gemBonus"] = level.Scoring.GemBonus
                };
            }

            if (level.TutorialSteps != null)
            {
                var steps = new JArray();
                foreach (var s in level.TutorialSteps)
                {
                    steps.Add(new JObject
                    {
                        ["topText"] = s.TopText ?? "",
                        ["bottomText"] = s.BottomText ?? "",
                        ["startX"] = s.StartX,
                        ["startY"] = s.StartY,
                        ["endX"] = s.EndX,
                        ["endY"] = s.EndY
                    });
                }
                root["tutorialSteps"] = steps;
            }

            if (level.Blobs != null)
            {
                var blobs = new JArray();
                foreach (var b in level.Blobs)
                {
                    var blobObj = new JObject
                    {
                        ["t"] = LevelDataKeys.Types.GetKeyFromBlobType(b.Type),
                        ["x"] = b.GridPosition.x,
                        ["y"] = b.GridPosition.y
                    };
                    if (BlobUsesColor(b.Type) && b.Color != BlobColor.Blank && b.Color != BlobColor.None)
                        blobObj["c"] = LevelDataKeys.BlobColors.GetKeyFromBlobColor(b.Color);
                    if (BlobUsesSize(b.Type) && b.Size != BlobSize.None)
                        blobObj["s"] = LevelDataKeys.BlobSizes.GetKeyFromBlobSize(b.Size);
                    if (BlobUsesTrailColor(b.Type))
                    {
                        var tc = b.GetProperty<BlobColor>(LevelDataKeys.Properties.TrailColor);
                        blobObj["tc"] = LevelDataKeys.BlobColors.GetKeyFromBlobColor(tc);
                    }
                    blobs.Add(blobObj);
                }
                root["blobs"] = blobs;
            }

            if (level.Tiles != null)
            {
                var tiles = new JArray();
                foreach (var t in level.Tiles)
                {
                    var tileObj = new JObject
                    {
                        ["t"] = LevelDataKeys.Types.GetKeyFromTileType(t.Type),
                        ["x"] = t.GridPosition.x,
                        ["y"] = t.GridPosition.y
                    };
                    if (TileUsesLaserId(t.Type))
                        tileObj["id"] = t.GetProperty<string>(LevelDataKeys.Properties.LaserId) ?? "";
                    if (TileUsesLaserColor(t.Type))
                        tileObj["c"] = LevelDataKeys.BlobColors.GetKeyFromBlobColor(t.GetProperty<BlobColor>(LevelDataKeys.Properties.Color));
                    tiles.Add(tileObj);
                }
                root["tiles"] = tiles;
            }

            if (level.LaserLinks != null && level.LaserLinks.Count > 0)
            {
                var links = new JArray();
                foreach (var link in level.LaserLinks)
                {
                    links.Add(new JObject
                    {
                        ["IdA"] = link.IdA ?? "",
                        ["IdB"] = link.IdB ?? "",
                        ["Color"] = link.Color ?? ""
                    });
                }
                root["laserLinks"] = links;
            }

            return root.ToString();
        }

        public static void FromJson(LevelData level, string json)
        {
            if (level == null || string.IsNullOrEmpty(json)) return;
            var root = JObject.Parse(json);

            level.LevelNumber = root["levelNum"]?.Value<int>() ?? 0;
            level.LevelName = level.LevelName ?? "";
            level.Width = root["width"]?.Value<int>() ?? 3;
            level.Height = root["height"]?.Value<int>() ?? 3;
            level.MinMoves = root["minMoves"]?.Value<int>() ?? 0;
            level.IsTutorial = root["isTutorial"]?.Value<bool>() ?? false;

            var scoringToken = root["scoring"];
            if (scoringToken is JObject scoringObj && level.Scoring != null)
            {
                level.Scoring.BaseScore = scoringObj["baseScore"]?.Value<int>() ?? 0;
                level.Scoring.MovePenalty = scoringObj["movePenalty"]?.Value<int>() ?? 0;
                var arr = scoringObj["starThresholds"] as JArray;
                if (arr != null)
                {
                    level.Scoring.StarThresholds = new int[arr.Count];
                    for (int i = 0; i < arr.Count; i++)
                        level.Scoring.StarThresholds[i] = arr[i].Value<int>();
                }
                level.Scoring.GemBonus = scoringObj["gemBonus"]?.Value<int>() ?? 0;
            }

            var stepsToken = root["tutorialSteps"];
            if (stepsToken is JArray stepsArray && stepsArray.Count > 0)
            {
                level.TutorialSteps = new TutorialStep[stepsArray.Count];
                for (int i = 0; i < stepsArray.Count; i++)
                {
                    var s = stepsArray[i] as JObject;
                    if (s == null) continue;
                    level.TutorialSteps[i] = new TutorialStep
                    {
                        TopText = s["topText"]?.Value<string>() ?? "",
                        BottomText = s["bottomText"]?.Value<string>() ?? "",
                        StartX = s["startX"]?.Value<int>() ?? 0,
                        StartY = s["startY"]?.Value<int>() ?? 0,
                        EndX = s["endX"]?.Value<int>() ?? 0,
                        EndY = s["endY"]?.Value<int>() ?? 0
                    };
                }
            }

            level.Blobs = new List<BlobSpawnData>();
            var blobsToken = root["blobs"];
            if (blobsToken is JArray blobsArray)
            {
                foreach (var bToken in blobsArray)
                {
                    if (bToken is not JObject b) continue;
                    string tKey = b["t"]?.Value<string>() ?? "nb";
                    int x = b["x"]?.Value<int>() ?? 0;
                    int y = b["y"]?.Value<int>() ?? 0;
                    BlobType type;
                    try { type = LevelDataKeys.Types.GetBlobTypeFromKey(tKey); }
                    catch { type = BlobType.Normal; }
                    BlobColor color = BlobColor.Blank;
                    string cKey = b["c"]?.Value<string>();
                    if (!string.IsNullOrEmpty(cKey))
                        color = LevelDataKeys.BlobColors.GetBlobColorFromKey(cKey);
                    BlobSize size = BlobSize.Normal;
                    string sKey = b["s"]?.Value<string>();
                    if (!string.IsNullOrEmpty(sKey))
                        size = LevelDataKeys.BlobSizes.GetBlobSizeFromKey(sKey);
                    if (!BlobUsesColor(type)) color = BlobColor.Blank;
                    if (!BlobUsesSize(type)) size = (type == BlobType.Ghost ? BlobSize.None : BlobSize.Normal);
                    var blob = new BlobSpawnData
                    {
                        GridPosition = new Vector2Int(x, y),
                        Type = type,
                        Color = color,
                        Size = size
                    };
                    string tcKey = b["tc"]?.Value<string>();
                    if (BlobUsesTrailColor(type) && !string.IsNullOrEmpty(tcKey))
                        blob.SetProperty(LevelDataKeys.Properties.TrailColor, LevelDataKeys.BlobColors.GetBlobColorFromKey(tcKey));
                    level.Blobs.Add(blob);
                }
            }

            level.Tiles = new List<TileSpawnData>();
            var tilesToken = root["tiles"];
            if (tilesToken is JArray tilesArray)
            {
                foreach (var tToken in tilesArray)
                {
                    if (tToken is not JObject t) continue;
                    string tKey = t["t"]?.Value<string>() ?? "nt";
                    int x = t["x"]?.Value<int>() ?? 0;
                    int y = t["y"]?.Value<int>() ?? 0;
                    TileType type;
                    try { type = LevelDataKeys.Types.GetTileTypeFromKey(tKey); }
                    catch { type = TileType.Normal; }
                    var tile = new TileSpawnData
                    {
                        GridPosition = new Vector2Int(x, y),
                        Type = type
                    };
                    var idToken = t["id"]?.Value<string>();
                    if (TileUsesLaserId(type) && !string.IsNullOrEmpty(idToken))
                        tile.SetProperty(LevelDataKeys.Properties.LaserId, idToken);
                    var cKey = t["c"]?.Value<string>();
                    if (TileUsesLaserColor(type) && !string.IsNullOrEmpty(cKey))
                        tile.SetProperty(LevelDataKeys.Properties.Color, LevelDataKeys.BlobColors.GetBlobColorFromKey(cKey));
                    level.Tiles.Add(tile);
                }
            }

            var linksToken = root["laserLinks"];
            if (linksToken is JArray linksArray && linksArray.Count > 0)
            {
                if (level.LaserLinks == null) level.LaserLinks = new List<LaserLink>();
                level.LaserLinks.Clear();
                foreach (var lToken in linksArray)
                {
                    if (lToken is not JObject l) continue;
                    level.LaserLinks.Add(new LaserLink
                    {
                        IdA = l["IdA"]?.Value<string>() ?? "",
                        IdB = l["IdB"]?.Value<string>() ?? "",
                        Color = l["Color"]?.Value<string>() ?? ""
                    });
                }
            }
        }
    }
}
