namespace PaperHeroes
{
    /// <summary>Unit archetype. Healer is STRETCH (M5) — type exists, MVP does not spawn it.</summary>
    public enum Role
    {
        Tank,
        MeleeDealer,
        RangedDealer,
        Healer
    }

    /// <summary>Which side a unit/base belongs to.</summary>
    public enum Team
    {
        Ally,
        Enemy
    }
}
