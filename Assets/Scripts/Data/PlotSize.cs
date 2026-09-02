/// <summary>
/// Preset plot sizes offered at world creation. Values are the side length
/// of the (square) plot in tiles; the plot is centered on world origin, so
/// bounds are ±(value/2). Don't renumber — the integer values are persisted
/// in <see cref="WorldSaveData.plotSideTiles"/>.
///
/// Reasoning for the sizes:
/// - Small (256): cozy office-idle plot; ~5 seconds to sprint corner-to-corner;
///   fits 3-4 biome regions once Phase 5 lands.
/// - Medium (512): default; ~17 seconds to sprint; 5-8 biomes; comfortable
///   room to explore without minimap dependence.
/// - Large (1024): wilderness feel; ~34 seconds to sprint; 15-20 biomes;
///   minimap navigation encouraged.
/// </summary>
public enum PlotSize
{
    Small  = 256,
    Medium = 512,
    Large  = 1024,
}
