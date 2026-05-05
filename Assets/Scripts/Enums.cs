namespace AutoChess
{
    public enum UnitClass
    {
        Warrior,
        Mage,
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
    }
}
