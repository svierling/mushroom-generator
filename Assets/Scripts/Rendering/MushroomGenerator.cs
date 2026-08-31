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

    // Cached visible-tile AABB — regeneration is skipped when neither the AABB
    // nor the zoom has changed. See GroundTileGenerator for the same technique.
    private int lastMinTileX, lastMaxTileX, lastMinTileY, lastMaxTileY;
    private float lastOrthoSize;
    private bool hasCachedFrame;

    private void Start()
    {
        // Pre-warm object pool to avoid mid-frame allocations
        for (int i = 0; i < poolInitialSize; i++)
        {
            inactiveMushroomPool.Enqueue(InstantiateInactive());
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

    // Sprite half-size used to pad the visible region so mushrooms whose center
    // sits just outside the camera rect but whose sprite still peeks in don't
    // pop out at the edges.
    private const float VISIBILITY_MARGIN_UNITS = 2f;

    private void Update()
    {
        Vector3 cameraPos = mainCamera.transform.position;
        float halfWidthUnits = mainCamera.orthographicSize * mainCamera.aspect;
        float halfHeightUnits = mainCamera.orthographicSize;

        float minUnityX = cameraPos.x - halfWidthUnits - VISIBILITY_MARGIN_UNITS;
        float maxUnityX = cameraPos.x + halfWidthUnits + VISIBILITY_MARGIN_UNITS;
        float minUnityY = cameraPos.y - halfHeightUnits - VISIBILITY_MARGIN_UNITS;
        float maxUnityY = cameraPos.y + halfHeightUnits + VISIBILITY_MARGIN_UNITS;

        // The camera rectangle back-projects to a diamond in tile space.
        // The AABB of that diamond is the smallest rectangle of tiles we need
        // to consider; we then reject tiles whose iso projection falls outside
        // the camera rect. Corners map like this:
        //   worldX peaks at the top-right camera corner (max unityX + max unityY)
        //   worldY peaks at the top-left camera corner (min unityX + max unityY)
        // Union of back-projections at h=0 and h=MaxHeight so raised terrain
        // tiles (which shift up on screen) also make it into the tile AABB.
        // See GroundMeshRenderer for the same treatment.
        int maxH = TerrainService.Provider.MaxHeight;
        Vector2 tl0 = IsoProjection.UnityToWorld(minUnityX, maxUnityY, 0);
        Vector2 tr0 = IsoProjection.UnityToWorld(maxUnityX, maxUnityY, 0);
        Vector2 bl0 = IsoProjection.UnityToWorld(minUnityX, minUnityY, 0);
        Vector2 br0 = IsoProjection.UnityToWorld(maxUnityX, minUnityY, 0);
        Vector2 tlH = IsoProjection.UnityToWorld(minUnityX, maxUnityY, maxH);
        Vector2 trH = IsoProjection.UnityToWorld(maxUnityX, maxUnityY, maxH);
        Vector2 blH = IsoProjection.UnityToWorld(minUnityX, minUnityY, maxH);
        Vector2 brH = IsoProjection.UnityToWorld(maxUnityX, minUnityY, maxH);

        int minTileX = Mathf.FloorToInt(Mathf.Min(Mathf.Min(Mathf.Min(tl0.x, tr0.x), Mathf.Min(bl0.x, br0.x)),
                                                  Mathf.Min(Mathf.Min(tlH.x, trH.x), Mathf.Min(blH.x, brH.x))));
        int maxTileX = Mathf.CeilToInt(Mathf.Max(Mathf.Max(Mathf.Max(tl0.x, tr0.x), Mathf.Max(bl0.x, br0.x)),
                                                 Mathf.Max(Mathf.Max(tlH.x, trH.x), Mathf.Max(blH.x, brH.x))));
        int minTileY = Mathf.FloorToInt(Mathf.Min(Mathf.Min(Mathf.Min(tl0.y, tr0.y), Mathf.Min(bl0.y, br0.y)),
                                                  Mathf.Min(Mathf.Min(tlH.y, trH.y), Mathf.Min(blH.y, brH.y))));
        int maxTileY = Mathf.CeilToInt(Mathf.Max(Mathf.Max(Mathf.Max(tl0.y, tr0.y), Mathf.Max(bl0.y, br0.y)),
                                                 Mathf.Max(Mathf.Max(tlH.y, trH.y), Mathf.Max(blH.y, brH.y))));

        // Skip regeneration when visible AABB and zoom are unchanged; character
        // moving sub-tile amounts doesn't shift any tile in/out of view.
        if (hasCachedFrame &&
            minTileX == lastMinTileX && maxTileX == lastMaxTileX &&
            minTileY == lastMinTileY && maxTileY == lastMaxTileY &&
            Mathf.Approximately(mainCamera.orthographicSize, lastOrthoSize))
        {
            return;
        }
        lastMinTileX = minTileX; lastMaxTileX = maxTileX;
        lastMinTileY = minTileY; lastMaxTileY = maxTileY;
        lastOrthoSize = mainCamera.orthographicSize;
        hasCachedFrame = true;

        ReturnAllToPool();

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            int span = (maxTileX - minTileX + 1) * (maxTileY - minTileY + 1);
            Debug.Log($"Tile AABB: X[{minTileX},{maxTileX}] Y[{minTileY},{maxTileY}] = {span} tiles | Camera pos: {cameraPos}");
        }

        for (int worldSectorX = minTileX; worldSectorX <= maxTileX; worldSectorX++)
        {
            for (int worldSectorY = minTileY; worldSectorY <= maxTileY; worldSectorY++)
            {
                // Cheap iso-visibility test: project the tile and reject if
                // the projection falls outside the padded camera rect.
                TerrainSample sample = TerrainService.SampleAt(worldSectorX, worldSectorY);
                Vector3 unityPos = IsoProjection.WorldToUnity(worldSectorX, worldSectorY, sample.height);
                if (unityPos.x < minUnityX || unityPos.x > maxUnityX ||
                    unityPos.y < minUnityY || unityPos.y > maxUnityY)
                {
                    continue;
                }

                MushroomData data = MushroomData.Generate((uint)worldSectorX, (uint)worldSectorY);
                if (!data.exists) continue;

                MushroomInstance mushroom = GetFromPool();
                Sprite sprite = spriteData.GetSprite(data.type);
                mushroom.Configure(worldSectorX, worldSectorY, sample.height, sprite);
            }
        }

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Active mushrooms: {activeMushroomPool.Count} | Pool size: {inactiveMushroomPool.Count}");
        }
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
            instance = inactiveMushroomPool.Dequeue();
        }
        else
        {
            instance = InstantiateInactive();
            if (showDebugInfo)
            {
                Debug.LogWarning($"Object pool exhausted! Creating new instance. Consider increasing poolInitialSize. Current active: {activeMushroomPool.Count}");
            }
        }

        instance.gameObject.SetActive(true);
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

    // Only used by Start()/GetFromPool. Returns a fresh instance that is neither
    // pooled nor active — callers own the placement.
    private MushroomInstance InstantiateInactive()
    {
        GameObject obj = Instantiate(mushroomPrefab, mushroomContainer);
        obj.SetActive(false);
        MushroomInstance instance = obj.GetComponent<MushroomInstance>();

        if (instance == null)
        {
            Debug.LogError("Mushroom prefab is missing MushroomInstance component!");
        }

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

        // Draw iso tile centers around the camera
        Gizmos.color = Color.cyan * 0.3f;
        Vector2 centerTile = IsoProjection.UnityToWorld(cameraPos.x, cameraPos.y);
        int centerTileX = Mathf.RoundToInt(centerTile.x);
        int centerTileY = Mathf.RoundToInt(centerTile.y);
        const int gridSize = 10;
        for (int x = centerTileX - gridSize; x < centerTileX + gridSize; x++)
        {
            for (int y = centerTileY - gridSize; y < centerTileY + gridSize; y++)
            {
                Gizmos.DrawWireSphere(IsoProjection.WorldToUnity(x, y), 0.1f);
            }
        }
    }
#endif
}
