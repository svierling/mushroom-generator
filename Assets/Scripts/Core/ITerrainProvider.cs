/// <summary>
/// Source of terrain data. Consumers read through <see cref="TerrainService"/>
/// so the provider can be swapped (flat -> heightmap) without touching them.
/// </summary>
public interface ITerrainProvider
{
    TerrainSample SampleAt(int worldX, int worldY);

    /// <summary>
    /// Upper bound on the height value <see cref="SampleAt"/> can return for
    /// any tile. Rendering uses it to widen the visible tile AABB — raised
    /// tiles project higher on screen, so tiles further away in world-tile
    /// coordinates can still land inside the camera view. Return 0 for flat
    /// worlds so no extra tiles get iterated.
    /// </summary>
    int MaxHeight { get; }
}
