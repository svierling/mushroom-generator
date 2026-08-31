/// <summary>
/// Terrain slope shape for a single tile.
///
/// Flat means the tile is level; the four edge slopes raise one edge; the
/// four corner slopes raise one corner. Only Flat is produced today —
/// the rest are reserved for Phase 0.75 (real terrain generation).
/// </summary>
public enum SlopeType
{
    Flat,
    NorthUp,
    EastUp,
    SouthUp,
    WestUp,
    NE_Up,
    SE_Up,
    SW_Up,
    NW_Up,
}
