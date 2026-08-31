/// <summary>
/// Sanity-check terrain provider: three east-rising plateaus along the X axis.
/// Not used by shipped code — kept in-tree as a smoke-test utility so future
/// changes to the height-aware rendering pipeline can be verified without
/// having to hand-write a provider each time.
///
/// Every four tiles east, the ground steps up one level. At tile x=0 through
/// x=3 the height is 0, x=4..7 is 1, x=8..11 is 2, and so on. Y has no effect.
/// </summary>
public sealed class StaircaseTerrainProvider : ITerrainProvider
{
    public TerrainSample SampleAt(int worldX, int worldY)
    {
        int height = System.Math.Max(0, worldX / 4);
        return new TerrainSample(height, SlopeType.Flat);
    }

    // Big enough to cover any staircase the player is likely to walk during a
    // smoke test. Height grows unbounded with X in this provider, but the
    // AABB widening only needs to cover the range of heights currently in
    // frame — a generous cap here keeps the renderer honest.
    public int MaxHeight => 200;
}
