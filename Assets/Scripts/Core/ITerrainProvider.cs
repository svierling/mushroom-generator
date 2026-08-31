/// <summary>
/// Source of terrain data. Consumers read through <see cref="TerrainService"/>
/// so the provider can be swapped (flat -> heightmap) without touching them.
/// </summary>
public interface ITerrainProvider
{
    TerrainSample SampleAt(int worldX, int worldY);
}
