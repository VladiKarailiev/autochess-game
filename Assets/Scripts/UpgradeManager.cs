using System.Collections.Generic;
using UnityEngine;

namespace AutoChess
{
    public class UpgradeManager : MonoBehaviour
    {
        public BoardGrid grid;

        public void CheckUpgrades()
        {
            int safety = 32;
            while (TryMergeOne() && --safety > 0) { }
        }

        bool TryMergeOne()
        {
            var allUnits = Object.FindObjectsByType<Unit>(FindObjectsInactive.Exclude);
            var byKey = new Dictionary<(UnitData, int), List<Unit>>();

            for (int i = 0; i < allUnits.Length; i++)
            {
                var u = allUnits[i];
                if (u == null || u.team != Team.Player || u.data == null) continue;
                if (u.tier >= 3) continue;

                var key = (u.data, u.tier);
                if (!byKey.TryGetValue(key, out var list))
                {
                    list = new List<Unit>(3);
                    byKey[key] = list;
                }
                list.Add(u);

                if (list.Count >= 3)
                {
                    MergeThree(list);
                    return true;
                }
            }
            return false;
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
