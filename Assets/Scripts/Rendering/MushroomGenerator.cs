using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main generation system for mushroom rendering.
/// Implements sector-based procedural generation matching the C++ olcPixelGameEngine implementation.
/// Regenerates visible mushrooms every frame using object pooling for performance.
/// </summary>
public class MushroomGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform mushroomContainer;
    [SerializeField] private MushroomSpriteData spriteData;
    [SerializeField] private GameObject mushroomPrefab;

    [Header("Performance Settings")]
    [Tooltip("Initial size of the object pool. Increase if pool expands frequently during play.")]
    [SerializeField] private int poolInitialSize = 100;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Object pool for mushroom instances
    private List<MushroomInstance> activeMushroomPool = new List<MushroomInstance>();
    private Queue<MushroomInstance> inactiveMushroomPool = new Queue<MushroomInstance>();

    // Constants matching C++ implementation
    private const int SECTOR_SIZE_PIXELS = 16;  // 16x16 pixel sectors
    private const int PIXELS_PER_UNIT = 16;     // PPU setting for sprites

    private void Start()
    {
        // Pre-warm object pool to avoid mid-frame allocations
        for (int i = 0; i < poolInitialSize; i++)
        {
            CreateNewMushroomInstance();
        }

        if (mainCamera == null)
        {
            Debug.LogError("MushroomGenerator: Main Camera reference is missing!");
        }

        if (mushroomContainer == null)
        {
            Debug.LogError("MushroomGenerator: Mushroom Container reference is missing!");
        }

        if (spriteData == null)
        {
            Debug.LogError("MushroomGenerator: Sprite Data reference is missing!");
        }

        if (mushroomPrefab == null)
        {
            Debug.LogError("MushroomGenerator: Mushroom Prefab reference is missing!");
        }
    }

    private void Update()
    {
        // Calculate visible sectors based on camera viewport
        int visibleSectorsX = CalculateVisibleSectorsX();
        int visibleSectorsY = CalculateVisibleSectorsY();

        // Calculate camera view area in sector coordinates
        Vector3 cameraPos = mainCamera.transform.position;
        float halfWidthPixels = (mainCamera.orthographicSize * mainCamera.aspect) * PIXELS_PER_UNIT;
        float halfHeightPixels = mainCamera.orthographicSize * PIXELS_PER_UNIT;

        float topLeftPixelX = (cameraPos.x * PIXELS_PER_UNIT) - halfWidthPixels;
        float topLeftPixelY = (cameraPos.y * PIXELS_PER_UNIT) - halfHeightPixels;

        int sectorOffsetX = Mathf.FloorToInt(topLeftPixelX / SECTOR_SIZE_PIXELS);
        int sectorOffsetY = Mathf.FloorToInt(topLeftPixelY / SECTOR_SIZE_PIXELS);

        // Return all active mushrooms to pool
        ReturnAllToPool();

        // Debug output
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Visible sectors: {visibleSectorsX}x{visibleSectorsY} | " +
                      $"Sector offset: ({sectorOffsetX}, {sectorOffsetY}) | " +
                      $"Camera pos: {cameraPos}");
        }

        // Generate for each visible sector (EXACT C++ match)
        // This replicates the C++ OnUserUpdate loop at lines 148-196
        for (int screenX = 0; screenX < visibleSectorsX; screenX++)
        {
            for (int screenY = 0; screenY < visibleSectorsY; screenY++)
            {
                // Calculate world sector coordinates
                // Keep as signed integers to properly handle negative coordinates
                int worldSectorX = sectorOffsetX + screenX;
                int worldSectorY = sectorOffsetY + screenY;

                // Generate mushroom deterministically
                // Cast to uint only when passing to Generate (matches C++ seed calculation)
                MushroomData data = MushroomData.Generate((uint)worldSectorX, (uint)worldSectorY);

                if (data.exists)
                {
                    // Mushroom sits on top of the terrain at this tile. Height is
                    // 0 today via FlatTerrainProvider; the plumbing is here so
                    // Phase 0.75 can drop in a heightmap without touching this loop.
                    TerrainSample sample = TerrainService.SampleAt(worldSectorX, worldSectorY);

                    MushroomInstance mushroom = GetFromPool();
                    Sprite sprite = spriteData.GetSprite(data.type);
                    mushroom.Configure(worldSectorX, worldSectorY, sample.height, sprite);
                }
            }
        }

        // Debug: Show active mushroom count
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Active mushrooms: {activeMushroomPool.Count} | Pool size: {inactiveMushroomPool.Count}");
        }
    }

    /// <summary>
    /// Calculate number of visible sectors horizontally based on camera viewport.
    /// Matches C++ calculation: nSectorsX = ScreenWidth() / 16
    /// </summary>
    private int CalculateVisibleSectorsX()
    {
        // Camera orthographic size is half-height in world units
        // Visible width = 2 * orthographicSize * aspect ratio
        float visibleWidthUnits = mainCamera.orthographicSize * 2f * mainCamera.aspect;
        float visibleWidthPixels = visibleWidthUnits * PIXELS_PER_UNIT;
        // Add 2 extra sectors to handle partial sectors and movement
        return Mathf.CeilToInt(visibleWidthPixels / SECTOR_SIZE_PIXELS) + 2;
    }

    /// <summary>
    /// Calculate number of visible sectors vertically based on camera viewport.
    /// Matches C++ calculation: nSectorsY = ScreenHeight() / 16
    /// </summary>
    private int CalculateVisibleSectorsY()
    {
        // Visible height = 2 * orthographicSize
        float visibleHeightUnits = mainCamera.orthographicSize * 2f;
        float visibleHeightPixels = visibleHeightUnits * PIXELS_PER_UNIT;
        // Add 2 extra sectors to handle partial sectors and movement
        return Mathf.CeilToInt(visibleHeightPixels / SECTOR_SIZE_PIXELS) + 2;
    }

    /// <summary>
    /// Get a mushroom instance from the object pool.
    /// Creates a new instance if pool is exhausted.
    /// </summary>
    private MushroomInstance GetFromPool()
    {
        MushroomInstance instance;

        if (inactiveMushroomPool.Count > 0)
        {
            // Reuse from pool
            instance = inactiveMushroomPool.Dequeue();
            instance.gameObject.SetActive(true);
        }
        else
        {
            // Pool exhausted - create new instance
            instance = CreateNewMushroomInstance();

            if (showDebugInfo)
            {
                Debug.LogWarning($"Object pool exhausted! Creating new instance. Consider increasing poolInitialSize. Current active: {activeMushroomPool.Count}");
            }
        }

        activeMushroomPool.Add(instance);
        return instance;
    }

    /// <summary>
    /// Return all active mushrooms to the inactive pool.
    /// Called every frame before regenerating visible mushrooms.
    /// </summary>
    private void ReturnAllToPool()
    {
        foreach (var instance in activeMushroomPool)
        {
            instance.gameObject.SetActive(false);
            inactiveMushroomPool.Enqueue(instance);
        }
        activeMushroomPool.Clear();
    }

    /// <summary>
    /// Create a new mushroom instance for the object pool.
    /// </summary>
    private MushroomInstance CreateNewMushroomInstance()
    {
        GameObject obj = Instantiate(mushroomPrefab, mushroomContainer);
        obj.SetActive(false);
        MushroomInstance instance = obj.GetComponent<MushroomInstance>();

        if (instance == null)
        {
            Debug.LogError("Mushroom prefab is missing MushroomInstance component!");
        }

        inactiveMushroomPool.Enqueue(instance);
        return instance;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Draw debug gizmos in Scene view to visualize sector grid.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo || mainCamera == null) return;

        // Draw camera viewport bounds
        Gizmos.color = Color.yellow;
        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float halfHeight = mainCamera.orthographicSize;
        Vector3 cameraPos = mainCamera.transform.position;

        Gizmos.DrawWireCube(
            new Vector3(cameraPos.x, cameraPos.y, 0),
            new Vector3(halfWidth * 2f, halfHeight * 2f, 0)
        );

        // Draw sector grid (only show a small area around camera)
        Gizmos.color = Color.cyan * 0.3f;
        int gridSize = 10;
        int cameraSectorX = Mathf.FloorToInt(cameraPos.x);
        int cameraSectorY = Mathf.FloorToInt(cameraPos.y);

        for (int x = cameraSectorX - gridSize; x < cameraSectorX + gridSize; x++)
        {
            for (int y = cameraSectorY - gridSize; y < cameraSectorY + gridSize; y++)
            {
                Vector3 sectorPos = new Vector3(x, y, 0);
                Gizmos.DrawWireCube(sectorPos, Vector3.one);
            }
        }
    }
#endif
}
