using System.Collections.Generic;

namespace AutoChess
{
    public struct SynergyResult
    {
        public int warriorCount;
        public int mageCount;
        public int humanCount;
        public int beastCount;

        public int WarriorTier => TierFromCount(warriorCount);
        public int MageTier    => TierFromCount(mageCount);
        public int HumanTier   => TierFromCount(humanCount);
        public int BeastTier   => TierFromCount(beastCount);

        public static int TierFromCount(int count)
        {
            if (count >= 3) return 2;
            if (count >= 2) return 1;
            return 0;
        }
    }

    public static class SynergyEngine
    {
        public static float WarriorHp(int tier)  => tier switch { 1 => 1.20f, 2 => 1.50f, _ => 1f };
        public static float MageDamage(int tier) => tier switch { 1 => 1.25f, 2 => 1.60f, _ => 1f };
        public static float HumanAspd(int tier)  => tier switch { 1 => 1.15f, 2 => 1.35f, _ => 1f };
        public static float BeastDamage(int tier)=> tier switch { 1 => 1.20f, 2 => 1.45f, _ => 1f };

        public static string DescribeWarrior(int tier) => tier switch { 1 => "+20% HP",   2 => "+50% HP",  _ => "—" };
        public static string DescribeMage(int tier)    => tier switch { 1 => "+25% DMG",  2 => "+60% DMG", _ => "—" };
        public static string DescribeHuman(int tier)   => tier switch { 1 => "+15% ASPD", 2 => "+35% ASPD",_ => "—" };
        public static string DescribeBeast(int tier)   => tier switch { 1 => "+20% DMG",  2 => "+45% DMG", _ => "—" };

        public static SynergyResult ComputeFromBoard(BoardGrid grid)
        {
            var classSet = new Dictionary<UnitClass, HashSet<string>>();
            var typeSet  = new Dictionary<UnitType,  HashSet<string>>();

            foreach (var t in grid.AllTiles())
            {
                if (t.zone != TileZone.PlayerCombat) continue;
                var u = t.occupant;
                if (u == null || u.team != Team.Player) continue;

                AddUnique(classSet, u.unitClass, u.displayName);
                AddUnique(typeSet,  u.unitType,  u.displayName);
            }

            return new SynergyResult
            {
                warriorCount = CountOf(classSet, UnitClass.Warrior),
                mageCount    = CountOf(classSet, UnitClass.Mage),
                humanCount   = CountOf(typeSet,  UnitType.Human),
                beastCount   = CountOf(typeSet,  UnitType.Beast),
            };
        }

        public static SynergyResult Compute(IReadOnlyList<Unit> units)
        {
            var classSet = new Dictionary<UnitClass, HashSet<string>>();
            var typeSet  = new Dictionary<UnitType,  HashSet<string>>();

            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u == null || u.team != Team.Player) continue;
                AddUnique(classSet, u.unitClass, u.displayName);
                AddUnique(typeSet,  u.unitType,  u.displayName);
            }

            return new SynergyResult
            {
                warriorCount = CountOf(classSet, UnitClass.Warrior),
                mageCount    = CountOf(classSet, UnitClass.Mage),
                humanCount   = CountOf(typeSet,  UnitType.Human),
                beastCount   = CountOf(typeSet,  UnitType.Beast),
            };
        }

        public static void Apply(IReadOnlyList<Unit> units, SynergyResult r)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u == null) continue;

                float hp   = 1f;
                float dmg  = 1f;
                float aspd = 1f;

                if (u.unitClass == UnitClass.Warrior) hp   *= WarriorHp(r.WarriorTier);
                if (u.unitClass == UnitClass.Mage)    dmg  *= MageDamage(r.MageTier);
                if (u.unitType  == UnitType.Human)    aspd *= HumanAspd(r.HumanTier);
                if (u.unitType  == UnitType.Beast)    dmg  *= BeastDamage(r.BeastTier);

                u.ApplyCombatBuffs(hp, dmg, aspd);
            }
        }

        static void AddUnique<TKey>(Dictionary<TKey, HashSet<string>> dict, TKey key, string id)
        {
            if (!dict.TryGetValue(key, out var set))
                dict[key] = set = new HashSet<string>();
            set.Add(id);
        }

        static int CountOf<TKey>(Dictionary<TKey, HashSet<string>> dict, TKey key)
        {
            return dict.TryGetValue(key, out var set) ? set.Count : 0;
        }
    }
}
