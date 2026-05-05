using UnityEngine;

namespace AutoChess
{
    public class Shop : MonoBehaviour
    {
        public PlayerEconomy economy;
        public BoardGrid grid;

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
            if (bench == null) return false;

            if (!economy.TrySpend(data.cost)) return false;
            SpawnUnit(data, bench);
            slotPurchased[slotIndex] = true;
            return true;
        }

        public void Sell(Unit unit)
        {
            if (unit == null || unit.data == null) return;
            economy.Gain(unit.data.cost);
            if (unit.CurrentTile != null)
                unit.CurrentTile.occupant = null;
            Destroy(unit.gameObject);
        }

        void SpawnUnit(UnitData data, Tile tile)
        {
            var go = new GameObject();
            go.transform.SetParent(grid.transform, false);
            var unit = go.AddComponent<Unit>();
            unit.Initialize(data, Team.Player);
            unit.PlaceOnTile(tile);
        }
    }
}
