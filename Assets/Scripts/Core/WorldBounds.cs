using UnityEngine;

/// <summary>
/// Static accessor for the active world's plot bounds. Reads from
/// <see cref="WorldManager.Instance.CurrentWorld"/>; falls back to a very
/// large sentinel when no world is loaded so callers in the main menu
/// scene (where no gameplay clamping should happen anyway) don't
/// null-ref.
///
/// Plots are square and centered on world origin: bounds are ±(side/2).
/// The half extent is exclusive of the outermost row/column so tiles at
/// exactly (halfExtent, y) or (x, halfExtent) are out of bounds — that
/// tile row is where the cliff face renders.
/// </summary>
public static class WorldBounds
{
    // Sentinel bounds when no world is loaded — large enough that legacy
    // tests and pre-boot callers behave as if the world were infinite.
    private const int SENTINEL_HALF_EXTENT = 1 << 20;

    /// <summary>Half the plot side length in tiles. Bounds are (-HalfExtent, +HalfExtent).</summary>
    public static int HalfExtent
    {
        get
        {
            if (WorldManager.Instance != null && WorldManager.Instance.HasActiveWorld)
            {
                int side = WorldManager.Instance.CurrentWorld.plotSideTiles;
                if (side > 0) return side / 2;
            }
            return SENTINEL_HALF_EXTENT;
        }
    }

    public static int MinX => -HalfExtent;
    public static int MaxX =>  HalfExtent - 1;
    public static int MinY => -HalfExtent;
    public static int MaxY =>  HalfExtent - 1;

    /// <summary>Whether a tile coordinate falls inside the playable plot.</summary>
    public static bool Contains(int tileX, int tileY)
    {
        int half = HalfExtent;
        return tileX >= -half && tileX < half && tileY >= -half && tileY < half;
    }

    /// <summary>Clamp a float tile position to the playable plot. Use for character/camera positioning.</summary>
    public static Vector2 Clamp(float tileX, float tileY)
    {
        // Leave a hair of margin from the outermost cell so the character's
        // sprite base doesn't hang over the cliff edge.
        float max = HalfExtent - 1.0f;
        float min = -HalfExtent;
        return new Vector2(
            Mathf.Clamp(tileX, min, max),
            Mathf.Clamp(tileY, min, max));
    }

    /// <summary>Clamp an integer tile position to the playable plot.</summary>
    public static Vector2Int ClampInt(int tileX, int tileY)
    {
        return new Vector2Int(
            Mathf.Clamp(tileX, MinX, MaxX),
            Mathf.Clamp(tileY, MinY, MaxY));
    }
}
