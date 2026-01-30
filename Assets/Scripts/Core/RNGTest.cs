using UnityEngine;

/// <summary>
/// Test script to verify RNG implementation matches C++ version.
/// Attach to any GameObject and run in Play mode to see console output.
///
/// Compare these results with C++ output to verify deterministic generation.
/// </summary>
public class RNGTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private bool detailedOutput = true;

    private void Start()
    {
        if (runOnStart)
        {
            RunTests();
        }
    }

    public void RunTests()
    {
        Debug.Log("=== MUSHROOM GENERATOR RNG VERIFICATION TEST ===");
        Debug.Log("Compare these results with C++ output for the same coordinates.");
        Debug.Log("");

        // Test known coordinates
        TestSector(0, 0);
        TestSector(10, 5);
        TestSector(255, 255);
        TestSector(1000, 1000);
        TestSector(100, 200);

        Debug.Log("");
        Debug.Log("=== TEST COMPLETE ===");
        Debug.Log("If results match C++ output, RNG implementation is correct!");

        // Additional detailed test
        if (detailedOutput)
        {
            Debug.Log("");
            TestMushroomDistribution();
        }
    }

    private void TestSector(uint x, uint y)
    {
        var mushroom = MushroomData.Generate(x, y);

        string result = $"Sector ({x,4},{y,4}): ";

        if (mushroom.exists)
        {
            result += $"EXISTS | Type: {mushroom.GetName(),-12} | Rarity: {mushroom.GetRarity()}";
        }
        else
        {
            result += "NO MUSHROOM";
        }

        Debug.Log(result);
    }

    /// <summary>
    /// Test mushroom spawn distribution over a large area.
    /// Verifies approximate spawn rates match expected probabilities.
    /// </summary>
    private void TestMushroomDistribution()
    {
        Debug.Log("=== DISTRIBUTION TEST (1000 sectors) ===");

        int totalSectors = 1000;
        int mushroomsFound = 0;
        int bolete = 0;
        int roundhead = 0;
        int chanterelle = 0;

        for (uint i = 0; i < totalSectors; i++)
        {
            var mushroom = MushroomData.Generate(i, i);

            if (mushroom.exists)
            {
                mushroomsFound++;

                switch (mushroom.type)
                {
                    case MushroomData.MushroomType.Bolete:
                        bolete++;
                        break;
                    case MushroomData.MushroomType.Roundhead:
                        roundhead++;
                        break;
                    case MushroomData.MushroomType.Chanterelle:
                        chanterelle++;
                        break;
                }
            }
        }

        float spawnRate = (float)mushroomsFound / totalSectors * 100f;
        Debug.Log($"Mushrooms found: {mushroomsFound}/{totalSectors} ({spawnRate:F2}%)");
        Debug.Log($"Expected spawn rate: ~1.43% (1/70)");
        Debug.Log("");

        if (mushroomsFound > 0)
        {
            float boletePercent = (float)bolete / mushroomsFound * 100f;
            float roundheadPercent = (float)roundhead / mushroomsFound * 100f;
            float chanterellePercent = (float)chanterelle / mushroomsFound * 100f;

            Debug.Log("Type Distribution (of mushrooms that exist):");
            Debug.Log($"  Bolete (Red):        {bolete,3} ({boletePercent:F1}%) - Expected ~41.7%");
            Debug.Log($"  Roundhead (Green):   {roundhead,3} ({roundheadPercent:F1}%) - Expected ~50.0%");
            Debug.Log($"  Chanterelle (Yellow): {chanterelle,3} ({chanterellePercent:F1}%) - Expected ~16.7%");
        }
    }

    /// <summary>
    /// Call this from Inspector button or code to run tests again.
    /// </summary>
    [ContextMenu("Run RNG Tests")]
    public void RunTestsFromMenu()
    {
        RunTests();
    }
}
