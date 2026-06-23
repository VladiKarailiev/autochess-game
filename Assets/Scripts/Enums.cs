namespace AutoChess
{
    public enum UnitClass
    {
        Warrior,
        Mage,
        Ranger,
    }

    public enum UnitType
    {
        Human,
        Beast,
    }

    public enum TileZone
    {
        Bench,
        PlayerCombat,
        EnemyCombat,
    }

    public enum Team
    {
        Player,
        Enemy,
    }

    public enum GamePhase
    {
        Prep,
        Combat,
        Result,
        GameOver,
    }

    public enum ItemStat
    {
        Health,
        Damage,
        AttackSpeed,
        Range,
    }

    public enum AbilityKind
    {
        None,
        ShieldWall,    // self shield that absorbs damage
        Bloodlust,     // self attack-speed buff
        Maul,          // bonus damage on next hit
        Fireball,      // AoE damage around the current target
        Heal,          // heal the lowest-HP ally
        Regrowth,      // heal self over time
        ToxicShot,     // poison the target over time
        PiercingShot,  // instant bonus damage to the target
        Volley,        // damage the 3 nearest enemies
    }
}
