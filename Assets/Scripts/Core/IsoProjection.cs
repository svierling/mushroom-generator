using UnityEngine;

/// <summary>
/// Faked 2D isometric projection with a 2:1 tile ratio.
///
/// World coordinates stay integer cartesian (x, y) sector tiles — the same
/// coordinates the RNG and MushroomData use. This class applies the iso
/// projection only at render time, so game logic (spawning, RNG, terrain
/// sampling) stays axis-aligned.
///
/// Height is an integer in "levels" (1 level = one step up). Every tile
/// carries a height even when the world is flat (all zero today); the
/// plumbing is here so real terrain generation is a data-source swap.
/// </summary>
public static class IsoProjection
{
    public const int TILE_WIDTH_PIXELS = 32;
    public const int TILE_HEIGHT_PIXELS = 16;
    public const int HEIGHT_STEP_PIXELS = 8;
    public const int PIXELS_PER_UNIT = 16;

    // Multiplied into sortingOrder to break ties between tiles at the same
    // (x + y) so a raised tile draws in front of a flat tile behind it.
    public const int HEIGHT_SORT_WEIGHT = 10;

    private const float TILE_HALF_WIDTH_UNITS  = (TILE_WIDTH_PIXELS  / 2f) / PIXELS_PER_UNIT;
    private const float TILE_HALF_HEIGHT_UNITS = (TILE_HEIGHT_PIXELS / 2f) / PIXELS_PER_UNIT;
    private const float HEIGHT_STEP_UNITS      = (float)HEIGHT_STEP_PIXELS / PIXELS_PER_UNIT;

    /// <summary>
    /// Project a world tile (x, y, height) into Unity world-space position.
    /// Accepts float tile coordinates so sub-tile positions (e.g. the character
    /// walking between tiles) project smoothly.
    /// </summary>
    public static Vector3 WorldToUnity(float worldX, float worldY, float height = 0f)
    {
        float unityX = (worldX - worldY) * TILE_HALF_WIDTH_UNITS;
        float unityY = (worldX + worldY) * TILE_HALF_HEIGHT_UNITS + height * HEIGHT_STEP_UNITS;
        return new Vector3(unityX, unityY, 0f);
    }

    /// <summary>
    /// Inverse of <see cref="WorldToUnity"/> at a known height.
    ///
    /// Mouse picking uses this in a walk-down loop: try the maximum possible
    /// height first, project a tile down to Unity space, check whether the
    /// cursor is above that tile; if not, decrement height and try again. On
    /// flat terrain the loop is one iteration.
    /// </summary>
    public static Vector2 UnityToWorld(float unityX, float unityY, float height = 0f)
    {
        float adjustedUnityY = unityY - height * HEIGHT_STEP_UNITS;
        float worldX = (unityX / TILE_HALF_WIDTH_UNITS + adjustedUnityY / TILE_HALF_HEIGHT_UNITS) * 0.5f;
        float worldY = (adjustedUnityY / TILE_HALF_HEIGHT_UNITS - unityX / TILE_HALF_WIDTH_UNITS) * 0.5f;
        return new Vector2(worldX, worldY);
    }

    /// <summary>
    /// Height at a sub-tile position, given the terrain sample for the tile the
    /// character is standing on. Flat-only today: returns <c>sample.height</c>.
    ///
    /// When slopes ship (Phase 0.75) this becomes a bilinear blend of the four
    /// corner heights. Callers are wired for that future — they pass fractional
    /// coords already.
    /// </summary>
    public static float HeightAtSubtile(int worldTileX, int worldTileY, float tileFracX, float tileFracY, TerrainSample sample)
    {
        return sample.height;
    }

    /// <summary>
    /// Sort order for a sprite at a given world tile + height. Higher (x + y)
    /// draws in front; height breaks ties within the same diagonal.
    /// </summary>
    public static int SortOrder(float worldX, float worldY, float height = 0f)
    {
        return -(int)((worldX + worldY) * 100f + height * HEIGHT_SORT_WEIGHT);
    }
}
