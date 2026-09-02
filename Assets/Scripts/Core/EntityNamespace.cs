/// <summary>
/// Salt values that keep RNG streams for different entity kinds independent
/// at the same (worldSeed, x, y). Without this, adding a second entity kind
/// (e.g. trees) at the same tile would consume rolls from the mushroom stream,
/// so a change to one generator's roll count would silently shift the other's
/// output.
///
/// Values must not change once shipped — they participate in the seed hash,
/// so renumbering would re-roll every world's content. Append new kinds at
/// the end.
///
/// <see cref="Mushroom"/> is deliberately 0 so its hash contribution is a
/// no-op — this preserves bit-for-bit RNG parity with pre-namespacing worlds.
/// </summary>
public enum EntityNamespace : byte
{
    Mushroom = 0,
    Tree     = 1,
    Ore      = 2,
    Mob      = 3,
    Npc      = 4,
    Foliage  = 5,
    Resource = 6,
}
