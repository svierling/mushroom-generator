using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders the whole ground grid as a single dynamic mesh — one draw call, no
/// GameObject-per-tile overhead. Replaces GroundTileGenerator's pooled sprite
/// approach; the AABB cache from that iteration is preserved so the mesh only
/// rebuilds when the visible tile set actually changes (i.e. when the character
/// crosses a tile boundary or zoom changes).
///
/// Uses a runtime-generated iso diamond texture at the exact tile pixel size,
/// so the mesh is just camera-facing quads with UV mapping. All quads share
/// one material → single draw call regardless of tile count.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GroundMeshRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Colors")]
    [SerializeField] private Color fillColor   = new Color(0.30f, 0.65f, 0.20f, 1f);
    [SerializeField] private Color borderColor = new Color(0.20f, 0.47f, 0.13f, 1f);
    [Range(0f, 0.5f)]
    [Tooltip("Border thickness as a fraction of the diamond radius. ~0.06 is a 1-2 pixel line at the current tile size.")]
    [SerializeField] private float borderThickness = 0.06f;

    [Header("Layering")]
    [Tooltip("Sorting order for the whole ground mesh. Set well below the smallest expected mushroom sort key so ground always draws behind.")]
    [SerializeField] private int sortingOrder = -1_000_000;

    private const float VISIBILITY_MARGIN_UNITS = 2f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private Material material;

    // Same AABB cache as MushroomGenerator/GroundTileGenerator — regen only
    // when the visible tile set or zoom actually changes.
    private int lastMinTileX, lastMaxTileX, lastMinTileY, lastMaxTileY;
    private float lastOrthoSize;
    private bool hasCachedFrame;

    // Reused each rebuild to avoid GC.
    private readonly List<Vector3> vertexBuf   = new List<Vector3>(8192);
    private readonly List<Vector2> uvBuf       = new List<Vector2>(8192);
    private readonly List<int>     triangleBuf = new List<int>(12288);

    private void Awake()
    {
        meshFilter   = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh { name = "GroundMesh" };
        mesh.MarkDynamic();
        // UInt32 indices — a big AABB at 0.5x zoom can easily blow past the
        // 65k-vert limit of the default 16-bit format.
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.sharedMesh = mesh;

        Texture2D tex = BuildDiamondTexture(fillColor, borderColor, borderThickness);
        material = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
        meshRenderer.sharedMaterial = material;
        meshRenderer.sortingOrder = sortingOrder;

        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null) return;

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

        RebuildMesh(minTileX, maxTileX, minTileY, maxTileY, minUnityX, maxUnityX, minUnityY, maxUnityY);
    }

    private void RebuildMesh(int minTileX, int maxTileX, int minTileY, int maxTileY,
                             float minUnityX, float maxUnityX, float minUnityY, float maxUnityY)
    {
        vertexBuf.Clear();
        uvBuf.Clear();
        triangleBuf.Clear();

        // Tile quad half-size — 2:1 iso, 32×16 px at PPU 16 → (1.0, 0.5) units.
        float halfW = IsoProjection.TILE_WIDTH_PIXELS  * 0.5f / IsoProjection.PIXELS_PER_UNIT;
        float halfH = IsoProjection.TILE_HEIGHT_PIXELS * 0.5f / IsoProjection.PIXELS_PER_UNIT;

        for (int worldX = minTileX; worldX <= maxTileX; worldX++)
        {
            for (int worldY = minTileY; worldY <= maxTileY; worldY++)
            {
                TerrainSample sample = TerrainService.SampleAt(worldX, worldY);
                Vector3 center = IsoProjection.WorldToUnity(worldX, worldY, sample.height);
                if (center.x < minUnityX || center.x > maxUnityX ||
                    center.y < minUnityY || center.y > maxUnityY)
                {
                    continue;
                }

                int v0 = vertexBuf.Count;

                // Camera-facing quad; UV maps to the full diamond texture.
                vertexBuf.Add(new Vector3(center.x - halfW, center.y - halfH, 0f));
                vertexBuf.Add(new Vector3(center.x - halfW, center.y + halfH, 0f));
                vertexBuf.Add(new Vector3(center.x + halfW, center.y + halfH, 0f));
                vertexBuf.Add(new Vector3(center.x + halfW, center.y - halfH, 0f));

                uvBuf.Add(new Vector2(0f, 0f));
                uvBuf.Add(new Vector2(0f, 1f));
                uvBuf.Add(new Vector2(1f, 1f));
                uvBuf.Add(new Vector2(1f, 0f));

                triangleBuf.Add(v0);     triangleBuf.Add(v0 + 1); triangleBuf.Add(v0 + 2);
                triangleBuf.Add(v0);     triangleBuf.Add(v0 + 2); triangleBuf.Add(v0 + 3);
            }
        }

        mesh.Clear();
        mesh.SetVertices(vertexBuf);
        mesh.SetUVs(0, uvBuf);
        mesh.SetTriangles(triangleBuf, 0);
        mesh.RecalculateBounds();
    }

    private static Texture2D BuildDiamondTexture(Color fill, Color border, float borderThickness)
    {
        int w = IsoProjection.TILE_WIDTH_PIXELS;
        int h = IsoProjection.TILE_HEIGHT_PIXELS;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp,
        };

        Color[] pixels = new Color[w * h];
        Color transparent = new Color(0, 0, 0, 0);
        float cx = (w - 1) * 0.5f;
        float cy = (h - 1) * 0.5f;
        float hx = w * 0.5f;
        float hy = h * 0.5f;
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
        return tex;
    }
}
