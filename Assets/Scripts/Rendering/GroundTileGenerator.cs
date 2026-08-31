using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-frame regenerated pool of ground tiles covering the iso visible region.
/// Mirrors MushroomGenerator: back-projects the camera rect into tile space,
/// iterates the tile AABB, spawns one tile per sector inside the padded camera
/// rect. Every visible tile gets a sprite (no sparse density like mushrooms).
/// </summary>
public class GroundTileGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform tileContainer;
    [SerializeField] private GameObject groundTilePrefab;

    [Header("Sprites")]
    [Tooltip("Sprite used for flat tiles. If null, a solid-color diamond is generated at Start() at exactly the iso tile size.")]
    [SerializeField] private Sprite flatGrassSprite;

    [Tooltip("Fallback color used when flatGrassSprite is null.")]
    [SerializeField] private Color fallbackGrassColor = new Color(0.30f, 0.65f, 0.20f, 1f);

    [Tooltip("Border color drawn along the diamond edge of the fallback sprite. Set alpha to 0 to hide.")]
    [SerializeField] private Color fallbackBorderColor = new Color(0.20f, 0.47f, 0.13f, 1f);

    [Header("Performance Settings")]
    [Tooltip("Initial size of the object pool. Ground tiles are dense — sized to cover the visible diamond at the widest zoom without a first-frame allocation spike.")]
    [SerializeField] private int poolInitialSize = 1024;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private const float VISIBILITY_MARGIN_UNITS = 2f;

    private readonly List<GroundTileInstance> activePool = new List<GroundTileInstance>();
    private readonly Queue<GroundTileInstance> inactivePool = new Queue<GroundTileInstance>();

    // Cached AABB from the last regeneration. If the same range covers the
    // camera this frame, the existing tiles are still correct and we skip the
    // whole iteration. Camera moving sub-tile amounts won't trigger a rebuild
    // because the padded rect only picks up new tiles at integer boundaries.
    private int lastMinTileX, lastMaxTileX, lastMinTileY, lastMaxTileY;
    private float lastOrthoSize;
    private bool hasCachedFrame;

    private void Start()
    {
        if (flatGrassSprite == null)
        {
            flatGrassSprite = BuildDiamondSprite(fallbackGrassColor, fallbackBorderColor);
        }

        for (int i = 0; i < poolInitialSize; i++)
        {
            inactivePool.Enqueue(InstantiateInactive());
        }

        if (mainCamera == null)       Debug.LogError("GroundTileGenerator: Main Camera reference is missing!");
        if (tileContainer == null)    Debug.LogError("GroundTileGenerator: Tile Container reference is missing!");
        if (groundTilePrefab == null) Debug.LogError("GroundTileGenerator: Ground Tile Prefab reference is missing!");
    }

    /// <summary>
    /// Build a solid-color iso-diamond sprite at exactly TILE_WIDTH x TILE_HEIGHT
    /// pixels. Guarantees pixel-perfect tessellation regardless of what sprite
    /// asset (if any) the prefab has, so this stops being an art-configuration
    /// problem and starts being just a texture.
    /// </summary>
    private static Sprite BuildDiamondSprite(Color fill, Color border)
    {
        int w = IsoProjection.TILE_WIDTH_PIXELS;
        int h = IsoProjection.TILE_HEIGHT_PIXELS;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[w * h];
        Color transparent = new Color(0, 0, 0, 0);
        float cx = (w - 1) * 0.5f;
        float cy = (h - 1) * 0.5f;
        float hx = w * 0.5f;
        float hy = h * 0.5f;
        // Border thickness expressed as a fraction of the diamond radius so the
        // outline stays consistent even if tile pixel size changes later.
        // ~1-2 pixels wide at the current 32x16 tile.
        const float borderThickness = 0.06f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx) / hx;
                float dy = Mathf.Abs(y - cy) / hy;
                float radial = dx + dy;
                if (radial > 1f)
                {
                    pixels[y * w + x] = transparent;
                }
                else if (radial > 1f - borderThickness && border.a > 0f)
                {
                    pixels[y * w + x] = border;
                }
                else
                {
                    pixels[y * w + x] = fill;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            IsoProjection.PIXELS_PER_UNIT,
            0,
            SpriteMeshType.FullRect);
    }

    private void Update()
    {
        Vector3 cameraPos = mainCamera.transform.position;
        float halfWidthUnits = mainCamera.orthographicSize * mainCamera.aspect;
        float halfHeightUnits = mainCamera.orthographicSize;

        float minUnityX = cameraPos.x - halfWidthUnits - VISIBILITY_MARGIN_UNITS;
        float maxUnityX = cameraPos.x + halfWidthUnits + VISIBILITY_MARGIN_UNITS;
        float minUnityY = cameraPos.y - halfHeightUnits - VISIBILITY_MARGIN_UNITS;
        float maxUnityY = cameraPos.y + halfHeightUnits + VISIBILITY_MARGIN_UNITS;

        Vector2 tlTile = IsoProjection.UnityToWorld(minUnityX, maxUnityY);
        Vector2 trTile = IsoProjection.UnityToWorld(maxUnityX, maxUnityY);
        Vector2 blTile = IsoProjection.UnityToWorld(minUnityX, minUnityY);
        Vector2 brTile = IsoProjection.UnityToWorld(maxUnityX, minUnityY);

        int minTileX = Mathf.FloorToInt(Mathf.Min(Mathf.Min(tlTile.x, trTile.x), Mathf.Min(blTile.x, brTile.x)));
        int maxTileX = Mathf.CeilToInt(Mathf.Max(Mathf.Max(tlTile.x, trTile.x), Mathf.Max(blTile.x, brTile.x)));
        int minTileY = Mathf.FloorToInt(Mathf.Min(Mathf.Min(tlTile.y, trTile.y), Mathf.Min(blTile.y, brTile.y)));
        int maxTileY = Mathf.CeilToInt(Mathf.Max(Mathf.Max(tlTile.y, trTile.y), Mathf.Max(blTile.y, brTile.y)));

        // Skip regeneration when the visible tile range and zoom are unchanged.
        // Most frames the character has moved sub-tile amounts and the pool is
        // already correctly configured — huge win at 0.5x zoom where 2000+ tiles
        // otherwise get returned and re-spawned each frame for no visible change.
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

        for (int worldTileX = minTileX; worldTileX <= maxTileX; worldTileX++)
        {
            for (int worldTileY = minTileY; worldTileY <= maxTileY; worldTileY++)
            {
                TerrainSample sample = TerrainService.SampleAt(worldTileX, worldTileY);
                Vector3 unityPos = IsoProjection.WorldToUnity(worldTileX, worldTileY, sample.height);
                if (unityPos.x < minUnityX || unityPos.x > maxUnityX ||
                    unityPos.y < minUnityY || unityPos.y > maxUnityY)
                {
                    continue;
                }

                Sprite sprite = SpriteForSample(sample);
                GroundTileInstance tile = GetFromPool();
                tile.Configure(worldTileX, worldTileY, sample.height, sprite);
            }
        }

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Ground tiles active: {activePool.Count} | Pool free: {inactivePool.Count}");
        }
    }

    // Phase 0.75 grows this into a slope-shape lookup. Today every sample is
    // Flat so the flat sprite falls through.
    private Sprite SpriteForSample(TerrainSample sample)
    {
        return flatGrassSprite;
    }

    private GroundTileInstance GetFromPool()
    {
        GroundTileInstance instance;

        if (inactivePool.Count > 0)
        {
            instance = inactivePool.Dequeue();
        }
        else
        {
            instance = InstantiateInactive();
            if (showDebugInfo)
            {
                Debug.LogWarning($"GroundTileGenerator pool exhausted. Growing. Current active: {activePool.Count}");
            }
        }

        instance.gameObject.SetActive(true);
        activePool.Add(instance);
        return instance;
    }

    private void ReturnAllToPool()
    {
        foreach (var instance in activePool)
        {
            instance.gameObject.SetActive(false);
            inactivePool.Enqueue(instance);
        }
        activePool.Clear();
    }

    // Only used by Start()/GetFromPool. Returns a fresh instance that is neither
    // pooled nor active — callers own the placement.
    private GroundTileInstance InstantiateInactive()
    {
        GameObject obj = Instantiate(groundTilePrefab, tileContainer);
        obj.SetActive(false);
        GroundTileInstance instance = obj.GetComponent<GroundTileInstance>();

        if (instance == null)
        {
            Debug.LogError("Ground tile prefab is missing GroundTileInstance component!");
        }

        return instance;
    }
}
