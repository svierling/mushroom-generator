using UnityEngine;

/// <summary>
/// Data structure representing a mushroom at a specific world tile.
/// Handles procedural generation using deterministic RNG based on coordinates.
/// (Historical note: the C++ port referred to tiles as "sectors".)
/// </summary>
public struct MushroomData
{
    public bool exists;
    public MushroomType type;
    public Vector2Int tileCoords;

    // Component shape landed in this PR as minimal prep for ROADMAP Phase 2.
    // Populated from a preset lookup on the current 3 types today; Phase 2
    // will roll these independently against a 10×10×16 art set with rarity
    // weights, and <see cref="type"/> will become an aggregate label instead
    // of a driver of these values.
    public int capIndex;
    public int stemIndex;
    public int colorIndex;
    public Rarity capRarity;
    public Rarity stemRarity;
    public Rarity colorRarity;
    public Rarity overallRarity;

    /// <summary>
    /// Types of mushrooms with different rarities.
    /// </summary>
    public enum MushroomType
    {
        Bolete,      // Red mushroom - Common (default)
        Roundhead,   // Green mushroom - Common
        Chanterelle  // Yellow mushroom - Uncommon
    }

    /// <summary>
    /// Generate mushroom data for a specific world tile using procedural generation.
    /// Uses the world seed from WorldManager for different results per world.
    /// Falls back to coordinate-only seeding if no world is loaded (backwards compatibility).
    /// </summary>
    /// <param name="tileX">World X coordinate of the tile</param>
    /// <param name="tileY">World Y coordinate of the tile</param>
    /// <returns>MushroomData containing existence and type information</returns>
    public static MushroomData Generate(uint tileX, uint tileY)
    {
        // Create RNG with world seed (if available) and tile coordinates,
        // namespaced to Mushroom so future entity kinds (trees, ores, mobs)
        // get independent RNG streams for the same (x, y, worldSeed).
        ProceduralRNG rng;
        if (WorldManager.Instance != null && WorldManager.Instance.HasActiveWorld)
        {
            rng = new ProceduralRNG(WorldManager.Instance.WorldSeed, tileX, tileY, EntityNamespace.Mushroom);
        }
        else
        {
            // Fallback to original behavior for backwards compatibility.
            // Legacy constructor kept for RNGTest-style bit-for-bit C++ parity
            // checks; production paths always take the world-seed branch above.
            rng = new ProceduralRNG(tileX, tileY);
        }

        var data = new MushroomData
        {
            tileCoords = new Vector2Int((int)tileX, (int)tileY),
            // CRITICAL: 1 in 70 chance for mushroom to exist (matching C++)
            exists = (rng.RndInt(0, 70) == 1)
        };

        // If no mushroom exists, return early
        if (!data.exists)
            return data;

        // CRITICAL: Exact type determination logic from C++
        bool isType2 = (rng.RndInt(0, 2) == 1);  // 1/2 chance (50%)
        bool isType3 = (rng.RndInt(0, 6) == 1);  // 1/6 chance (~16.67%)

        // Determine mushroom type (priority: Type2 > Type3 > Default)
        if (isType2)
            data.type = MushroomType.Roundhead;
        else if (isType3)
            data.type = MushroomType.Chanterelle;
        else
            data.type = MushroomType.Bolete;

        // Component indices + rarities via preset lookup. Deterministic given
        // type, so RNG parity is preserved (no additional rolls consumed).
        MushroomPresets.Preset preset = MushroomPresets.For(data.type);
        data.capIndex      = preset.capIndex;
        data.stemIndex     = preset.stemIndex;
        data.colorIndex    = preset.colorIndex;
        data.capRarity     = preset.capRarity;
        data.stemRarity    = preset.stemRarity;
        data.colorRarity   = preset.colorRarity;
        data.overallRarity = preset.overallRarity;

        return data;
    }

    /// <summary>
    /// Get mushroom name as string.
    /// </summary>
    public string GetName()
    {
        return type switch
        {
            MushroomType.Bolete => "Bolete",
            MushroomType.Roundhead => "Roundhead",
            MushroomType.Chanterelle => "Chanterelle",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get rarity as string.
    /// </summary>
    public string GetRarity()
    {
        return type switch
        {
            MushroomType.Bolete => "Common",
            MushroomType.Roundhead => "Common",
            MushroomType.Chanterelle => "Uncommon",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get rarity color (Green for Common, Yellow for Uncommon).
    /// </summary>
    public Color GetRarityColor()
    {
        return type switch
        {
            MushroomType.Bolete => Color.green,
            MushroomType.Roundhead => Color.green,
            MushroomType.Chanterelle => Color.yellow,
            _ => Color.white
        };
    }

    /// <summary>
    /// Get edibility status as string.
    /// All mushroom types in this generator are edible.
    /// </summary>
    public string GetEdibility()
    {
        return type switch
        {
            MushroomType.Bolete => "Edible",
            MushroomType.Roundhead => "Edible",
            MushroomType.Chanterelle => "Edible",
            _ => "Unknown"
        };
    }
}
