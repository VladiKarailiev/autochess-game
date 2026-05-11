using UnityEngine;
using System.Collections.Generic;

namespace AutoChess
{
    public class BoardGrid : MonoBehaviour
    {
        [Header("Dimensions")]
        [Min(1)] public int columns = 6;
        [Min(1)] public int playerRows = 2;
        [Min(1)] public int enemyRows  = 2;
        [Min(0)] public int benchRows  = 1;
        [Min(0.1f)] public float tileSize = 1f;

        Tile[,] tiles;

        public int TotalRows => benchRows + playerRows + enemyRows;

        void Awake()
        {
            IndexTiles();
        }

        void IndexTiles()
        {
            int total = TotalRows;
            tiles = new Tile[columns, total];

            var allTiles = GetComponentsInChildren<Tile>(true);
            foreach (var tile in allTiles)
            {
                int x = tile.gridPos.x;
                int y = tile.gridPos.y;
                if (x < 0 || x >= columns || y < 0 || y >= total)
                {
                    Debug.LogError($"Tile '{tile.name}' has out-of-range gridPos {tile.gridPos}.", tile);
                    continue;
                }
                if (tiles[x, y] != null)
                {
                    Debug.LogError($"Two tiles share gridPos ({x},{y}): '{tiles[x, y].name}' and '{tile.name}'.", tile);
                    continue;
                }
                tiles[x, y] = tile;
            }

            int missing = 0;
            for (int x = 0; x < columns; x++)
                for (int y = 0; y < total; y++)
                    if (tiles[x, y] == null) missing++;
            if (missing > 0)
                Debug.LogError($"BoardGrid: {missing} tile slot(s) empty.");
        }

        public Vector3 GridToLocal(int x, int y)
        {
            int total = TotalRows;
            float offsetX = -(columns - 1) * 0.5f * tileSize;
            float offsetY = -(total - 1) * 0.5f * tileSize;
            return new Vector3(offsetX + x * tileSize, offsetY + y * tileSize, 0f);
        }

        public Tile GetTile(int x, int y)
        {
            if (x < 0 || x >= columns || y < 0 || y >= TotalRows) return null;
            return tiles[x, y];
        }

        public Tile WorldToTile(Vector3 worldPos)
        {
            Vector3 local = transform.InverseTransformPoint(worldPos);
            int total = TotalRows;

            Tile best = null;
            float bestDist = float.MaxValue;
            float halfTile = tileSize * 0.5f;

            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < total; y++)
                {
                    var tile = tiles[x, y];
                    if (tile == null) continue;
                    Vector3 tileLocal = transform.InverseTransformPoint(tile.transform.position);
                    float dx = local.x - tileLocal.x;
                    float dy = local.y - tileLocal.y;
                    if (Mathf.Abs(dx) > halfTile || Mathf.Abs(dy) > halfTile) continue;
                    float d = dx * dx + dy * dy;
                    if (d < bestDist) { bestDist = d; best = tile; }
                }
            }
            return best;
        }

        public int CountUnitsInZone(TileZone zone)
        {
            int count = 0;
            int total = TotalRows;
            for (int x = 0; x < columns; x++)
                for (int y = 0; y < total; y++)
                {
                    var t = tiles[x, y];
                    if (t != null && t.zone == zone && t.occupant != null) count++;
                }
            return count;
        }

        public int CountPlayerCombatUnits() => CountUnitsInZone(TileZone.PlayerCombat);

        public Tile FindFirstEmptyBenchTile()
        {
            for (int x = 0; x < columns; x++)
                for (int y = 0; y < benchRows; y++)
                {
                    var t = tiles[x, y];
                    if (t != null && t.zone == TileZone.Bench && t.occupant == null) return t;
                }
            return null;
        }

        public List<Tile> CollectEmptyTilesInZone(TileZone zone)
        {
            var list = new List<Tile>();
            int total = TotalRows;
            for (int x = 0; x < columns; x++)
                for (int y = 0; y < total; y++)
                {
                    var t = tiles[x, y];
                    if (t != null && t.zone == zone && t.occupant == null) list.Add(t);
                }
            return list;
        }

        public IEnumerable<Tile> AllTiles()
        {
            int total = TotalRows;
            for (int x = 0; x < columns; x++)
                for (int y = 0; y < total; y++)
                    if (tiles[x, y] != null) yield return tiles[x, y];
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
            int total = benchRows + playerRows + enemyRows;
            for (int x = 0; x < columns; x++)
                for (int y = 0; y < total; y++)
                {
                    Vector3 center = transform.TransformPoint(GridToLocal(x, y));
                    Gizmos.DrawWireCube(center, new Vector3(tileSize, tileSize, 0.01f));
                }
        }
    }
}
