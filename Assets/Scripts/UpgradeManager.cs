using System.Collections.Generic;
using UnityEngine;

namespace AutoChess
{
    public class UpgradeManager : MonoBehaviour
    {
        public BoardGrid grid;

        public void CheckUpgrades(Unit extraUnit = null)
        {
            int safety = 32;
            while (TryMergeOne(extraUnit) && --safety > 0) { }
        }

        bool TryMergeOne(Unit extraUnit)
        {
            var byKey = new Dictionary<(string, int), List<Unit>>();

            if (grid == null) return false;

            foreach (var tile in grid.AllTiles())
            {
                if (AddCandidate(tile.occupant, byKey)) return true;
            }

            if (extraUnit != null && extraUnit.CurrentTile == null)
                if (AddCandidate(extraUnit, byKey)) return true;

            return false;
        }

        bool AddCandidate(Unit unit, Dictionary<(string, int), List<Unit>> byKey)
        {
            if (unit == null || unit.team != Team.Player) return false;
            if (unit.tier >= 3) return false;

            var key = (unit.displayName, unit.tier);
            if (!byKey.TryGetValue(key, out var list))
            {
                list = new List<Unit>(3);
                byKey[key] = list;
            }
            list.Add(unit);

            if (list.Count < 3) return false;
            MergeThree(list);
            return true;
        }

        void MergeThree(List<Unit> three)
        {
            Unit keeper = three[0];
            for (int i = 1; i < three.Count; i++)
                if (BetterPlacement(three[i].CurrentTile, keeper.CurrentTile))
                    keeper = three[i];

            for (int i = 0; i < three.Count; i++)
            {
                var u = three[i];
                if (u == keeper) continue;
                if (u.CurrentTile != null && u.CurrentTile.occupant == u)
                    u.CurrentTile.occupant = null;
                Destroy(u.gameObject);
            }

            keeper.Upgrade();
        }

        static bool BetterPlacement(Tile a, Tile b)
        {
            if (a == null) return false;
            if (b == null) return true;
            if (a.zone != b.zone)
                return a.zone == TileZone.PlayerCombat;
            return a.gridPos.y > b.gridPos.y;
        }
    }
}
