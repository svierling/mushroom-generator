/// <summary>
/// Terrain provider that returns a flat, level world everywhere. Ships as the
/// default so Phase 0.5 renders correctly with no terrain generation yet.
/// Phase 0.75 replaces this with a heightmap-based provider.
/// </summary>
public sealed class FlatTerrainProvider : ITerrainProvider
{
    public TerrainSample SampleAt(int worldX, int worldY) => TerrainSample.Flat;
    public int MaxHeight => 0;
}
