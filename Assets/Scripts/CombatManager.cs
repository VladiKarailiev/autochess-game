using System.Collections.Generic;
using UnityEngine;

namespace AutoChess
{
    public class CombatManager : MonoBehaviour
    {
        public BoardGrid grid;
        public RoundManager rounds;

        [Header("Enemy generation")]
        [Tooltip("Drag the unit prefabs the enemy can spawn.")]
        public Unit[] enemyPool;
        [Min(0f)] public float maxBattleSeconds = 30f;

        readonly List<Unit> playerUnits = new();
        readonly List<Unit> enemyUnits = new();
        readonly List<Unit> allUnits   = new();
        readonly Dictionary<Unit, Vector2Int> playerHomes = new();

        bool inCombat;
        float battleClock;

        public void StartBattle(int round)
        {
            CollectPlayerUnits();
            SpawnEnemies(round);

            // Apply synergies before OnCombatStart so currentHealth picks up the buffed MaxHealth.
            var synergies = SynergyEngine.Compute(playerUnits);
            SynergyEngine.Apply(playerUnits, synergies);

            allUnits.Clear();
            allUnits.AddRange(playerUnits);
            allUnits.AddRange(enemyUnits);

            foreach (var u in allUnits)
                if (u != null) u.OnCombatStart();

            battleClock = 0f;
            inCombat = true;
        }

        void CollectPlayerUnits()
        {
            playerUnits.Clear();
            playerHomes.Clear();
            foreach (var t in grid.AllTiles())
            {
                if (t.zone == TileZone.PlayerCombat && t.occupant != null)
                {
                    var u = t.occupant;
                    playerUnits.Add(u);
                    playerHomes[u] = t.gridPos;
                }
            }
        }

        void SpawnEnemies(int round)
        {
            enemyUnits.Clear();
            if (enemyPool == null || enemyPool.Length == 0) return;

            int count = Mathf.Max(1, round);
            var emptyEnemyTiles = grid.CollectEmptyTilesInZone(TileZone.EnemyCombat);

            for (int i = 0; i < count && emptyEnemyTiles.Count > 0; i++)
            {
                var prefab = enemyPool[Random.Range(0, enemyPool.Length)];
                if (prefab == null) continue;

                int idx = Random.Range(0, emptyEnemyTiles.Count);
                var tile = emptyEnemyTiles[idx];
                emptyEnemyTiles.RemoveAt(idx);

                Unit unit = Instantiate(prefab, grid.transform);
                unit.Initialize(Team.Enemy);

                int targetTier = RollEnemyTier(round);
                while (unit.tier < targetTier) unit.Upgrade();

                unit.PlaceOnTile(tile);
                enemyUnits.Add(unit);
            }
        }

        static int RollEnemyTier(int round)
        {
            float t3 = Mathf.Clamp((round - 6) * 0.05f, 0f, 0.25f);
            float t2 = Mathf.Clamp((round - 3) * 0.10f, 0f, 0.50f);
            float r = Random.value;
            if (r < t3) return 3;
            if (r < t3 + t2) return 2;
            return 1;
        }

        void Update()
        {
            if (!inCombat) return;

            float dt = Time.deltaTime;
            battleClock += dt;

            for (int i = 0; i < allUnits.Count; i++)
            {
                var u = allUnits[i];
                if (u == null) continue;
                u.Tick(dt, allUnits);
            }

            bool anyPlayer = AnyAlive(playerUnits);
            bool anyEnemy  = AnyAlive(enemyUnits);

            if (!anyPlayer || !anyEnemy || battleClock >= maxBattleSeconds)
                EndBattle();
        }

        static bool AnyAlive(List<Unit> list)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].IsAlive) return true;
            return false;
        }

        void EndBattle()
        {
            inCombat = false;

            int kills = 0;
            int survivorTierSum = 0;
            foreach (var u in enemyUnits)
            {
                if (u == null) continue;
                if (!u.IsAlive) kills++;
                else survivorTierSum += u.tier;
            }

            bool playerSurvived = AnyAlive(playerUnits);
            bool enemyEliminated = !AnyAlive(enemyUnits);
            bool won = playerSurvived && enemyEliminated;

            foreach (var u in enemyUnits)
                if (u != null) Destroy(u.gameObject);
            enemyUnits.Clear();

            foreach (var u in playerUnits)
            {
                if (u == null) continue;
                u.ClearCombatBuffs();
                Tile home = null;
                if (playerHomes.TryGetValue(u, out var pos))
                    home = grid.GetTile(pos.x, pos.y);
                u.RestoreToTile(home);
            }
            playerUnits.Clear();
            playerHomes.Clear();
            allUnits.Clear();

            if (rounds != null) rounds.OnBattleEnded(won, kills, survivorTierSum);
        }
    }
}
