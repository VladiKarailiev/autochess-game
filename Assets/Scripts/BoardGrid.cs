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
        [Range(0f, 0.2f)] public float tileGap = 0.05f;

        [Header("Tile Colors")]
        public Color playerColorA = new Color(0.85f, 0.85f, 0.85f);
        public Color playerColorB = new Color(0.65f, 0.65f, 0.65f);
        public Color enemyColorA  = new Color(0.85f, 0.55f, 0.55f);
        public Color enemyColorB  = new Color(0.65f, 0.40f, 0.40f);
        public Color benchColor   = new Color(0.45f, 0.35f, 0.25f);

        Tile[,] tiles;

        int TotalRows => benchRows + playerRows + enemyRows;

        void Awake()
        {
            BuildGrid();
        }

        void BuildGrid()
        {
            int total = TotalRows;
            tiles = new Tile[columns, total];
            for (int x = 0; x < columns; x++)
                for (int y = 0; y < total; y++)
                    tiles[x, y] = CreateTile(x, y);
        }

        TileZone ZoneOfRow(int y)
        {
            if (y < benchRows) return TileZone.Bench;
            if (y < benchRows + playerRows) return TileZone.PlayerCombat;
            return TileZone.EnemyCombat;
        }

        Tile CreateTile(int x, int y)
        {
            TileZone zone = ZoneOfRow(y);

            var go = new GameObject($"Tile_{x}_{y}_{zone}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = GridToLocal(x, y);

            float visualSize = Mathf.Max(0.01f, tileSize - tileGap);
            go.transform.localScale = new Vector3(visualSize, visualSize, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprites.GetSquare();
            sr.color = ColorFor(x, y, zone);

            var tile = go.AddComponent<Tile>();
            tile.gridPos = new Vector2Int(x, y);
            tile.zone = zone;
            return tile;
        }

        Color ColorFor(int x, int y, TileZone zone)
        {
            bool checker = (x + y) % 2 == 0;
            switch (zone)
            {
                case TileZone.Bench:        return benchColor;
                case TileZone.PlayerCombat: return checker ? playerColorA : playerColorB;
                case TileZone.EnemyCombat:  return checker ? enemyColorA  : enemyColorB;
                default: return Color.white;
            }
        }

        public Vector3 GridToLocal(int x, int y)
        {
            int total = TotalRows;
            float offsetX = -(columns - 1) * 0.5f * tileSize;
            float offsetY = -(total - 1) * 0.5f * tileSize;
            return new Vector3(offsetX + x * tileSize, offsetY + y * tileSize, 0f);
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return transform.TransformPoint(GridToLocal(gridPos.x, gridPos.y));
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
                    Vector3 tilePos = GridToLocal(x, y);
                    float dx = local.x - tilePos.x;
                    float dy = local.y - tilePos.y;
                    if (Mathf.Abs(dx) > halfTile || Mathf.Abs(dy) > halfTile) continue;
                    float d = dx * dx + dy * dy;
                    if (d < bestDist) { bestDist = d; best = GetTile(x, y); }
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
