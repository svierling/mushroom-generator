using UnityEngine;

/// <summary>
/// Data structure representing a mushroom at a specific world sector.
/// Handles procedural generation using deterministic RNG based on coordinates.
/// </summary>
public struct MushroomData
{
    public bool exists;
    public MushroomType type;
    public Vector2Int sectorCoords;

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
    /// Generate mushroom data for a specific world sector using procedural generation.
    /// This method produces identical results to the C++ implementation for the same coordinates.
    /// </summary>
    /// <param name="sectorX">World X coordinate of the sector</param>
    /// <param name="sectorY">World Y coordinate of the sector</param>
    /// <returns>MushroomData containing existence and type information</returns>
    public static MushroomData Generate(uint sectorX, uint sectorY)
    {
        // Create RNG with sector coordinates as seed
        var rng = new ProceduralRNG(sectorX, sectorY);

        var data = new MushroomData
        {
            sectorCoords = new Vector2Int((int)sectorX, (int)sectorY),
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
