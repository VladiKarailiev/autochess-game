using UnityEngine;

namespace AutoChess
{
    public class Shop : MonoBehaviour
    {
        public PlayerEconomy economy;
        public BoardGrid grid;
        public UpgradeManager upgrades;

        [Header("Pool & rules")]
        [Tooltip("Drag the unit prefabs here.")]
        public Unit[] pool;
        [Min(1)] public int slotCount = 5;
        [Min(0)] public int refreshCost = 2;

        public Unit[] currentSlots;
        public bool[] slotPurchased;

        void Awake()
        {
            currentSlots  = new Unit[slotCount];
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
            var prefab = currentSlots[slotIndex];
            if (prefab == null || slotPurchased[slotIndex]) return false;
            if (!economy.CanAfford(prefab.cost)) return false;

            Tile bench = grid.FindFirstEmptyBenchTile();
            // Allow purchase with a full bench if it would trigger an immediate merge.
            if (bench == null && (upgrades == null || !WouldImmediatelyMerge(prefab.displayName))) return false;

            if (!economy.TrySpend(prefab.cost)) return false;
            Unit unit = SpawnUnit(prefab, bench);
            slotPurchased[slotIndex] = true;
            upgrades?.CheckUpgrades(unit);
            return true;
        }

        public void Sell(Unit unit)
        {
            if (unit == null) return;
            economy.Gain(unit.SellValue);
            if (unit.CurrentTile != null)
                unit.CurrentTile.occupant = null;
            Destroy(unit.gameObject);
        }

        bool WouldImmediatelyMerge(string kind)
        {
            int count = 0;
            foreach (var t in grid.AllTiles())
            {
                if (t.occupant == null) continue;
                var u = t.occupant;
                if (u.team != Team.Player) continue;
                if (u.displayName == kind && u.tier == 1) count++;
            }
            return count >= 2;
        }

        Unit SpawnUnit(Unit prefab, Tile tile)
        {
            Unit unit = Instantiate(prefab, grid.transform);
            unit.Initialize(Team.Player);

            if (tile != null)
                unit.PlaceOnTile(tile);
            else
                unit.transform.position = new Vector3(-100f, -100f, 0f);

            return unit;
        }
    }
}
