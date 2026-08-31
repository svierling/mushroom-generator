/// <summary>
/// Terrain data for a single tile. Ground rendering, mushroom placement,
/// character height, and mouse picking all read this.
/// </summary>
public readonly struct TerrainSample
{
    public readonly int height;
    public readonly SlopeType slope;

    public TerrainSample(int height, SlopeType slope)
    {
        this.height = height;
        this.slope = slope;
    }

    public static readonly TerrainSample Flat = new TerrainSample(0, SlopeType.Flat);
}
