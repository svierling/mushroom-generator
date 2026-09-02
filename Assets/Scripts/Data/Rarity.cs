/// <summary>
/// Rarity tiers for mushroom components and overall rarity. Integer values
/// participate in the "average of component rarities" calculation used to
/// derive a mushroom's overall rarity (see ROADMAP Phase 2). Don't renumber
/// once shipped — the values are stored in save data.
/// </summary>
public enum Rarity
{
    Common   = 1,
    Uncommon = 2,
    Rare     = 3,
    VeryRare = 4,
    Anomaly  = 5,
}
