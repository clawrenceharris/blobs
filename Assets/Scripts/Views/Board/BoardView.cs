using UnityEngine;
using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Views
{
    /// <summary>
    /// View component for grid visual representation.
    /// Handles tile creation and blob view spawning.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float tileSize = 1.2f;

        [Header("Prefabs")]
        [SerializeField] private GameObject _tilePrefab;
        [SerializeField] private GameObject _normalBlobPrefab;
        [SerializeField] private GameObject _trailBlobPrefab;
        [SerializeField] private GameObject _ghostBlobPrefab;
        [SerializeField] private GameObject _flagBlobPrefab;
        [SerializeField] private GameObject _rockBlobPrefab;
        [SerializeField] private GameObject _switchBlobPrefab;
        private BoardPresenter _board;
        private BoardModel _boardModel;

        private readonly Dictionary<string, TileView> _tileViews = new();
        private readonly Dictionary<string, BlobView> _blobViews = new();

        public float TileSize => tileSize;


        public void Initialize(BoardPresenter board, BoardModel model)
        {
            _boardModel = model;
            _board = board;
        }
        public void CreateTiles()
        {
            ClearTileViews();


            foreach (Blob blob in _boardModel.GetAllBlobs())
            {
                Vector2Int gridPos = new(blob.GridPosition.x, blob.GridPosition.y);
                Vector3 worldPos = _board.Layout.GridToWorldWithBlobOffset(gridPos);
                GameObject prefab = GetBlobPrefabFromType(blob.Type);

                GameObject blobObj = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                blobObj.name = $"Blob_{blob.Type}_{blob.GridPosition.x}_{blob.GridPosition.y}";
                if (!blobObj.TryGetComponent<BlobView>(out var blobView))
                    blobView = blobObj.AddComponent<BlobView>();
                blobView.Initialize(blob);
                _blobViews[blob.ID] = blobView;

            }
            foreach (Tile tile in _boardModel.GetAllTiles())
            {
                Vector2Int gridPos = new(tile.GridPosition.x, tile.GridPosition.y);
                Vector3 worldPos = _board.Layout.GridToWorld(gridPos);
                GameObject prefab = GetTilePrefabFromType(tile.Type);

                GameObject tileObj = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                if (!tileObj.TryGetComponent<TileView>(out var tileView))
                    tileView = tileObj.AddComponent<TileView>();
                tileView.Initialize(tile);
                _tileViews[tile.ID] = tileView;


            }
        }

        public void ClearTileViews()
        {
            foreach (var tile in _tileViews.Values)
            {
                if (tile != null)
                    Destroy(tile.gameObject);
            }
            _tileViews.Clear();
        }
        

        private GameObject GetBlobPrefabFromType(BlobType type)
        {
            return type switch
            {
                BlobType.Normal => _normalBlobPrefab,
                BlobType.Trail => _trailBlobPrefab,
                BlobType.Ghost => _ghostBlobPrefab,
                BlobType.Flag => _flagBlobPrefab,
                BlobType.Rock => _rockBlobPrefab,
                BlobType.Switch => _switchBlobPrefab,
                _ => _normalBlobPrefab
            };
        }

        private GameObject GetTilePrefabFromType(TileType type)
        {
            return type switch
            {
                TileType.Normal => _tilePrefab,
                
                _ => _tilePrefab
            };
        }


        private GameObject CreateDefaultTile(Vector3 position)
        {
            GameObject tileObj = new GameObject("Tile");
            tileObj.transform.position = position;
            tileObj.transform.parent = transform;

            SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = new Color(0.3f, 0.25f, 0.4f, 1f);
            tileObj.transform.localScale = Vector3.one * tileSize;

            return tileObj;
        }

        private GameObject CreateDefaultBlobView(Vector3 position)
        {
            GameObject blobObj = new GameObject("Blob");
            blobObj.transform.position = position;
            blobObj.transform.parent = transform;

            SpriteRenderer sr = blobObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.sortingOrder = 1;
            blobObj.transform.localScale = Vector3.one * (tileSize * 0.7f);

            CircleCollider2D collider = blobObj.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;

            return blobObj;
        }

        private Sprite CreateSquareSprite()
        {
            Texture2D texture = new Texture2D(32, 32);
            Color[] colors = new Color[32 * 32];

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float cornerRadius = 6f;
                    bool inCorner = false;

                    if (x < cornerRadius && y < cornerRadius)
                        inCorner = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius)) > cornerRadius;
                    else if (x >= 32 - cornerRadius && y < cornerRadius)
                        inCorner = Vector2.Distance(new Vector2(x, y), new Vector2(32 - cornerRadius, cornerRadius)) > cornerRadius;
                    else if (x < cornerRadius && y >= 32 - cornerRadius)
                        inCorner = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, 32 - cornerRadius)) > cornerRadius;
                    else if (x >= 32 - cornerRadius && y >= 32 - cornerRadius)
                        inCorner = Vector2.Distance(new Vector2(x, y), new Vector2(32 - cornerRadius, 32 - cornerRadius)) > cornerRadius;

                    colors[y * 32 + x] = inCorner ? Color.clear : Color.white;
                }
            }

            texture.SetPixels(colors);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }

        private Sprite CreateCircleSprite()
        {
            Texture2D texture = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];
            Vector2 center = new Vector2(32, 32);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    colors[y * 64 + x] = dist <= 30 ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(colors);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;

            return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        }
    }
}
