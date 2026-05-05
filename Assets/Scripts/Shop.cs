using UnityEngine;

namespace AutoChess
{
    public class Shop : MonoBehaviour
    {
        public PlayerEconomy economy;
        public BoardGrid grid;
        public UpgradeManager upgrades;

        [Header("Pool & rules")]
        public UnitData[] pool;
        [Min(1)] public int slotCount = 5;
        [Min(0)] public int refreshCost = 2;

        public UnitData[] currentSlots;
        public bool[] slotPurchased;

        void Awake()
        {
            currentSlots  = new UnitData[slotCount];
            slotPurchased = new bool[slotCount];
        }

        public void Roll()
        {
            if (pool == null || pool.Length == 0) return;
            for (int i = 0; i < slotCount; i++)
            {
                currentSlots[i]  = pool[Random.Range(0, pool.Length)];
                slotPurchased[i] = false;
            }
        }

        public bool TryRefresh()
        {
            if (!economy.TrySpend(refreshCost)) return false;
            Roll();
            return true;
        }

        public bool TryBuy(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotCount) return false;
            var data = currentSlots[slotIndex];
            if (data == null || slotPurchased[slotIndex]) return false;
            if (!economy.CanAfford(data.cost)) return false;

            Tile bench = grid.FindFirstEmptyBenchTile();
            // If bench is full but the new unit would immediately merge with two
            // existing tier-1 copies, allow the purchase: the merge will free a tile.
            if (bench == null && !WouldImmediatelyMerge(data)) return false;

            if (!economy.TrySpend(data.cost)) return false;
            SpawnUnit(data, bench);
            slotPurchased[slotIndex] = true;
            upgrades?.CheckUpgrades();
            return true;
        }

        public void Sell(Unit unit)
        {
            if (unit == null || unit.data == null) return;
            economy.Gain(unit.SellValue);
            if (unit.CurrentTile != null)
                unit.CurrentTile.occupant = null;
            Destroy(unit.gameObject);
        }

        bool WouldImmediatelyMerge(UnitData data)
        {
            int count = 0;
            foreach (var t in grid.AllTiles())
            {
                if (t.occupant == null) continue;
                var u = t.occupant;
                if (u.team != Team.Player) continue;
                if (u.data == data && u.tier == 1) count++;
            }
            return count >= 2;
        }

        void SpawnUnit(UnitData data, Tile tile)
        {
            var go = new GameObject();
            go.transform.SetParent(grid.transform, false);
            var unit = go.AddComponent<Unit>();
            unit.Initialize(data, Team.Player);

            if (tile != null)
                unit.PlaceOnTile(tile);
            else
                go.transform.position = new Vector3(-100f, -100f, 0f); // off-screen until merge
        }
    }
}
