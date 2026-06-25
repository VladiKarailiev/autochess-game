using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoChess
{
    [Serializable]
    public class ItemDef
    {
        public string itemName = "Item";
        [TextArea] public string description = "";
        public ItemStat stat;
        public float amount = 10f;
    }

    public class ItemManager : MonoBehaviour
    {
        [Tooltip("Every item that can drop as a reward.")]
        public List<ItemDef> pool = new();

        readonly List<ItemDef> pending = new();

        public int PendingCount => pending.Count;
        public bool HasPending => pending.Count > 0;

        public void RollReward()
        {
            if (pool == null || pool.Count == 0) return;
            // Only ever hold one pending reward; a new one replaces the old.
            pending.Clear();
            pending.Add(pool[UnityEngine.Random.Range(0, pool.Count)]);
        }

        public ItemDef PeekNextPending()
        {
            return pending.Count > 0 ? pending[0] : null;
        }

        public ItemDef TakeNextPending()
        {
            if (pending.Count == 0) return null;
            var item = pending[0];
            pending.RemoveAt(0);
            return item;
        }
    }
}
