/// <summary>
/// Preset component tuples for the three legacy mushroom types. Each preset
/// captures the (cap, stem, color) indices and per-component rarities that
/// today's <see cref="MushroomData.MushroomType"/> corresponds to.
///
/// Minimal-prep for ROADMAP Phase 2: the component shape lives on
/// <see cref="MushroomData"/> now, but until Phase 2 authors the 10 caps ×
/// 10 stems × 16 colors sprite set, every mushroom rolls to one of these
/// three presets. When Phase 2 lands, <see cref="MushroomData.Generate"/>
/// will roll components + rarities directly instead of picking a preset.
///
/// Bit-for-bit RNG parity: this mapping is deterministic given the type,
/// so preset-based generation produces identical MushroomType values (and
/// therefore identical UI output) to pre-refactor worlds.
/// </summary>
public static class MushroomPresets
{
    public readonly struct Preset
    {
        public readonly int capIndex;
        public readonly int stemIndex;
        public readonly int colorIndex;
        public readonly Rarity capRarity;
        public readonly Rarity stemRarity;
        public readonly Rarity colorRarity;
        public readonly Rarity overallRarity;

        public Preset(int capIndex, int stemIndex, int colorIndex,
                      Rarity capRarity, Rarity stemRarity, Rarity colorRarity,
                      Rarity overallRarity)
        {
            this.capIndex = capIndex;
            this.stemIndex = stemIndex;
            this.colorIndex = colorIndex;
            this.capRarity = capRarity;
            this.stemRarity = stemRarity;
            this.colorRarity = colorRarity;
            this.overallRarity = overallRarity;
        }
    }

    // All three current mushrooms share the same cap/stem shape (index 0)
    // because we only ship one cap sprite and one stem sprite today. The
    // color index distinguishes them, matching the three shipped sprites
    // (Red, Green, Yellow). Overall rarities preserve the pre-refactor UI
    // strings — Chanterelle stays "Uncommon" even though (1+1+1)/3 would
    // round to Common; Phase 2 will replace this with the real average.
    public static readonly Preset Bolete      = new Preset(0, 0, 0, Rarity.Common, Rarity.Common, Rarity.Common,   Rarity.Common);
    public static readonly Preset Roundhead   = new Preset(0, 0, 1, Rarity.Common, Rarity.Common, Rarity.Common,   Rarity.Common);
    public static readonly Preset Chanterelle = new Preset(0, 0, 2, Rarity.Common, Rarity.Common, Rarity.Uncommon, Rarity.Uncommon);

    public static Preset For(MushroomData.MushroomType type) => type switch
    {
        MushroomData.MushroomType.Bolete      => Bolete,
        MushroomData.MushroomType.Roundhead   => Roundhead,
        MushroomData.MushroomType.Chanterelle => Chanterelle,
        _                                     => Bolete,
    };
}
