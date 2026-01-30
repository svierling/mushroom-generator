/// <summary>
/// Procedural random number generator that exactly replicates the C++ implementation.
/// This ensures deterministic mushroom generation matching the original application.
/// Extended to support world seeds for different "generations".
/// </summary>
public class ProceduralRNG
{
    private uint nProcGen;

    /// <summary>
    /// Initialize RNG with spatial coordinates (legacy constructor for backwards compatibility).
    /// Seed calculation: (x & 0xFFFF) << 16 | (y & 0xFFFF)
    /// </summary>
    public ProceduralRNG(uint x, uint y)
    {
        // CRITICAL: Exact seed calculation from C++ to ensure determinism
        nProcGen = ((x & 0xFFFF) << 16) | (y & 0xFFFF);
    }

    /// <summary>
    /// Initialize RNG with world seed and spatial coordinates.
    /// Different world seeds produce different results for the same coordinates.
    /// </summary>
    /// <param name="worldSeed">Unique seed for this world/generation</param>
    /// <param name="x">Sector X coordinate</param>
    /// <param name="y">Sector Y coordinate</param>
    public ProceduralRNG(uint worldSeed, uint x, uint y)
    {
        // Combine world seed with coordinates for unique per-world generation
        // XOR the world seed with coordinates before applying the original formula
        uint seedX = x ^ worldSeed;
        uint seedY = y ^ (worldSeed >> 16 | worldSeed << 16); // Rotate world seed for Y
        nProcGen = ((seedX & 0xFFFF) << 16) | (seedY & 0xFFFF);
    }

    /// <summary>
    /// Core RNG algorithm using hash-based pseudo-random generation.
    /// Must match C++ implementation exactly for deterministic results.
    /// </summary>
    private uint Rnd()
    {
        // CRITICAL: These magic constants must match C++ exactly
        nProcGen += 0xe120fc15;                         // Step 1: Add constant
        ulong tmp = (ulong)nProcGen * 0x4a39b70d;      // Step 2: Multiply
        uint m1 = (uint)(tmp >> 32) ^ (uint)tmp;       // Step 3: XOR fold
        tmp = (ulong)m1 * 0x12fad5c9;                  // Step 4: Multiply again
        uint m2 = (uint)(tmp >> 32) ^ (uint)tmp;       // Step 5: XOR fold again
        return m2;
    }

    /// <summary>
    /// Generate random integer in range [min, max).
    /// </summary>
    public int RndInt(int min, int max)
    {
        return (int)(Rnd() % (uint)(max - min)) + min;
    }

    /// <summary>
    /// Generate random double in range [min, max).
    /// </summary>
    public double RndDouble(double min, double max)
    {
        return ((double)Rnd() / (double)(0x7FFFFFFF)) * (max - min) + min;
    }
}
