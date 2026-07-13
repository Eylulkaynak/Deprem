using System.Collections.Generic;
using UnityEngine;

namespace DepremOyunu
{
    /// <summary>
    /// LevelData'yı okuyup sahnede fiziksel karo grid'ini oluşturur.
    /// Kamera izometrik olduğu için karolar düz bir XZ düzleminde dizilir;
    /// "izometrik" görünüm kamera açısından gelir (bkz. IsometricCameraRig).
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Veri")]
        public LevelData level;

        [Header("Prefablar")]
        public Tile tilePrefab;
        [Tooltip("Goal karosunun üstüne konacak opsiyonel tabela/bayrak prefabı")]
        public GameObject goalDecorPrefab;

        [Header("Materyal Paleti (tüm karolara uygulanır, HazardInfo.tileMaterial varsa o üstün gelir)")]
        public Material openMat;
        public Material startMat;
        public Material goalMat;
        public Material defaultHazardMat;
        public Material decisionMat;
        public Material visitedMat;
        public Material hoverMat;

        private readonly Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();

        public IReadOnlyDictionary<Vector2Int, Tile> Tiles => _tiles;

        private void Awake()
        {
            Build();
        }

        public void Build()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            _tiles.Clear();

            if (level == null || tilePrefab == null)
            {
                Debug.LogError("GridManager: LevelData veya tilePrefab atanmamış.");
                return;
            }

            for (int r = 0; r < level.rows; r++)
            {
                for (int c = 0; c < level.cols; c++)
                {
                    var tileType = level.GetTileType(r, c);
                    var worldPos = GetWorldPosition(r, c);

                    var tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                    tile.name = $"Tile_{r}_{c}_{tileType}";
                    tile.row = r;
                    tile.col = c;
                    tile.type = tileType;

                    tile.openMat = openMat;
                    tile.startMat = startMat;
                    tile.goalMat = goalMat;
                    tile.decisionMat = decisionMat;
                    tile.visitedMat = visitedMat;
                    tile.hoverMat = hoverMat;

                    if (tileType == TileType.Hazard)
                    {
                        var hKey = level.GetHazardKey(r, c);
                        tile.hazardKey = hKey;
                        var info = level.FindHazard(hKey);
                        tile.hazardMat = (info != null && info.tileMaterial != null) ? info.tileMaterial : defaultHazardMat;
                    }

                    if (tileType == TileType.Decision)
                    {
                        tile.decisionKey = level.GetDecisionKey(r, c);
                    }

                    tile.ApplyBaseVisual();

                    if (tileType == TileType.Goal && goalDecorPrefab != null)
                    {
                        Instantiate(goalDecorPrefab, worldPos + Vector3.up * 0.05f, Quaternion.identity, tile.transform);
                    }

                    _tiles[new Vector2Int(r, c)] = tile;
                }
            }

            var startTile = GetTile(level.start.row, level.start.col);
            if (startTile != null) startTile.SetVisited(true);
        }

        public Vector3 GetWorldPosition(int row, int col)
        {
            float size = level.tileSize;
            return new Vector3(col * size, 0f, -row * size);
        }

        public Tile GetTile(int row, int col)
        {
            _tiles.TryGetValue(new Vector2Int(row, col), out var t);
            return t;
        }

        public Vector3 BoardCenter()
        {
            if (level == null) return Vector3.zero;
            return GetWorldPosition(level.rows / 2, level.cols / 2);
        }
    }
}
