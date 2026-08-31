/// <summary>
/// Global access point for the active <see cref="ITerrainProvider"/>. Defaults
/// to <see cref="FlatTerrainProvider"/>; swap via <see cref="Provider"/> setter
/// (e.g. Phase 0.75 will drop in a HeightmapTerrainProvider, or the staircase
/// smoke test in Sub-phase G).
/// </summary>
public static class TerrainService
{
    private static ITerrainProvider provider = new FlatTerrainProvider();

    public static ITerrainProvider Provider
    {
        get => provider;
        set => provider = value ?? new FlatTerrainProvider();
    }

    public static TerrainSample SampleAt(int worldX, int worldY) => provider.SampleAt(worldX, worldY);
}
