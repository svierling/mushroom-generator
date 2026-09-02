using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renders the whole ground grid as a single dynamic mesh — one MeshRenderer,
/// one submesh of filled diamond triangles. No textures, so there are no
/// pixel-level aliasing artifacts at any zoom level.
///
/// The AABB cache from the pooled implementation is preserved so the mesh only
/// rebuilds when the visible tile set actually changes (character crosses a
/// tile boundary or zoom changes).
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GroundMeshRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Colors")]
    [SerializeField] private Color fillColor   = new Color(0.30f, 0.65f, 0.20f, 1f);
    [SerializeField] private Color borderColor = new Color(0.26f, 0.58f, 0.17f, 1f);
    [Tooltip("Uncheck to hide the tile grid outlines.")]
    [SerializeField] private bool showBorders = true;

    [Header("Scatter Variation")]
    [Tooltip("Decorative sprite scattered on a fraction of tiles. If null, a procedural palette-matched tuft is generated from fillColor at Start. Rendered on a diamond quad matching the tile, so the corners of the source texture get cropped.")]
    [SerializeField] private Texture2D variationTexture;
    [Tooltip("Fraction of tiles that get the variation sprite. 0.05 = 5%. Deterministic per-tile so the placement is stable.")]
    [Range(0f, 1f)]
    [SerializeField] private float variationDensity = 0.05f;

    [Header("Layering")]
    [Tooltip("Sorting order for the whole ground mesh. Unity's Renderer.sortingOrder is Int16, so use its floor (-32768) — this guarantees the ground stays below any sprite whose sort key hasn't clamped.")]
    [SerializeField] private int sortingOrder = short.MinValue;

    private const float VISIBILITY_MARGIN_UNITS = 2f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private Material fillMat;
    private Material variationMat;
    private Material borderMat;

    private int lastMinTileX, lastMaxTileX, lastMinTileY, lastMaxTileY;
    private float lastOrthoSize;
    private bool hasCachedFrame;

    private readonly List<Vector3> vertexBuf      = new List<Vector3>(8192);
    private readonly List<int>     triangleBuf    = new List<int>(12288);
    private readonly List<int>     lineBuf        = new List<int>(16384);
    private readonly List<Vector3> varVertexBuf   = new List<Vector3>(1024);
    private readonly List<Vector2> varUvBuf       = new List<Vector2>(1024);
    private readonly List<int>     varTriangleBuf = new List<int>(1536);
    // Merged UV / remapped variation triangle buffers — hoisted from RebuildMesh
    // to avoid the per-regen List allocations we used to make there.
    private readonly List<Vector2> mergedUvBuf         = new List<Vector2>(9216);
    private readonly List<int>     remappedVarTrisBuf  = new List<int>(1536);

    private void Awake()
    {
        meshFilter   = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh { name = "GroundMesh" };
        mesh.MarkDynamic();
        mesh.indexFormat = IndexFormat.UInt32;
        meshFilter.sharedMesh = mesh;

        Shader spritesDefault = Shader.Find("Sprites/Default");
        // Sprites/Default falls through to the material color when no texture
        // is set, so no shader switch is needed for solid-colored geometry.
        fillMat = new Material(spritesDefault);
        borderMat = new Material(spritesDefault);

        // Fall back to a procedural palette-matched tuft when no texture is
        // wired up, so the variation always reads as "same grass, subtle
        // patch" rather than requiring an authored asset.
        Texture2D effectiveVariation = variationTexture != null
            ? variationTexture
            : BuildProceduralVariation(fillColor);
        variationMat = new Material(spritesDefault) { mainTexture = effectiveVariation };

        // Materials array must match the submeshes emitted in RebuildMesh:
        // 0 = fill, 1 = variation (if density > 0), then borders (if enabled).
        var mats = new List<Material> { fillMat };
        if (variationDensity > 0f) mats.Add(variationMat);
        if (showBorders)           mats.Add(borderMat);
        meshRenderer.sharedMaterials = mats.ToArray();
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

        // Raised tiles shift up on screen by (height * HEIGHT_STEP_PIXELS/PPU),
        // so tiles whose world-tile coords fall OUTSIDE the h=0 back-projection
        // can still project INSIDE the camera view when they carry height. Take
        // the union of back-projections at h=0 and h=MaxHeight to cover both.
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
        triangleBuf.Clear();
        lineBuf.Clear();
        varVertexBuf.Clear();
        varUvBuf.Clear();
        varTriangleBuf.Clear();
        mergedUvBuf.Clear();
        remappedVarTrisBuf.Clear();

        float halfW = IsoProjection.TILE_WIDTH_PIXELS  * 0.5f / IsoProjection.PIXELS_PER_UNIT; // 1.0
        float halfH = IsoProjection.TILE_HEIGHT_PIXELS * 0.5f / IsoProjection.PIXELS_PER_UNIT; // 0.5
        bool wantVariation = variationDensity > 0f;
        // 5% density → 1-in-20 tiles. Compare against the fixed threshold in
        // uint hash space so the placement is stable regardless of camera.
        uint variationCutoff = (uint)(variationDensity * uint.MaxValue);

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
                vertexBuf.Add(new Vector3(center.x,         center.y + halfH, 0f));
                vertexBuf.Add(new Vector3(center.x + halfW, center.y,         0f));
                vertexBuf.Add(new Vector3(center.x,         center.y - halfH, 0f));
                vertexBuf.Add(new Vector3(center.x - halfW, center.y,         0f));

                triangleBuf.Add(v0); triangleBuf.Add(v0 + 1); triangleBuf.Add(v0 + 2);
                triangleBuf.Add(v0); triangleBuf.Add(v0 + 2); triangleBuf.Add(v0 + 3);

                if (showBorders)
                {
                    lineBuf.Add(v0);     lineBuf.Add(v0 + 1);
                    lineBuf.Add(v0 + 1); lineBuf.Add(v0 + 2);
                    lineBuf.Add(v0 + 2); lineBuf.Add(v0 + 3);
                    lineBuf.Add(v0 + 3); lineBuf.Add(v0);
                }

                if (wantVariation && TileHash(worldX, worldY) < variationCutoff)
                {
                    // Diamond-shaped quad matching the tile exactly. Verts in
                    // top / right / bottom / left order; UVs sample the diamond
                    // region of the source texture (corners are cropped).
                    int vv = varVertexBuf.Count;
                    varVertexBuf.Add(new Vector3(center.x,         center.y + halfH, 0f));
                    varVertexBuf.Add(new Vector3(center.x + halfW, center.y,         0f));
                    varVertexBuf.Add(new Vector3(center.x,         center.y - halfH, 0f));
                    varVertexBuf.Add(new Vector3(center.x - halfW, center.y,         0f));
                    varUvBuf.Add(new Vector2(0.5f, 1.0f));
                    varUvBuf.Add(new Vector2(1.0f, 0.5f));
                    varUvBuf.Add(new Vector2(0.5f, 0.0f));
                    varUvBuf.Add(new Vector2(0.0f, 0.5f));
                    varTriangleBuf.Add(vv);     varTriangleBuf.Add(vv + 1); varTriangleBuf.Add(vv + 2);
                    varTriangleBuf.Add(vv);     varTriangleBuf.Add(vv + 2); varTriangleBuf.Add(vv + 3);
                }
            }
        }

        fillMat.color = fillColor;
        borderMat.color = borderColor;

        // Submesh layout has to match the materials array set in Awake:
        //   0 = fill, then (variation if any), then (borders if enabled).
        // Fill / border indices point into the fill verts; variation gets its
        // own verts appended after the fill verts and its triangle indices
        // remapped into the merged range.
        int fillVertCount = vertexBuf.Count;
        List<Vector3> allVerts = vertexBuf;
        if (wantVariation)
        {
            allVerts.AddRange(varVertexBuf);
            for (int i = 0; i < fillVertCount; i++) mergedUvBuf.Add(Vector2.zero);
            mergedUvBuf.AddRange(varUvBuf);
        }

        mesh.Clear();
        mesh.SetVertices(allVerts);
        if (wantVariation) mesh.SetUVs(0, mergedUvBuf);

        int submeshCount = 1;
        if (wantVariation) submeshCount++;
        if (showBorders)   submeshCount++;
        mesh.subMeshCount = submeshCount;

        int submesh = 0;
        mesh.SetTriangles(triangleBuf, submesh: submesh++);
        if (wantVariation)
        {
            for (int i = 0; i < varTriangleBuf.Count; i++)
                remappedVarTrisBuf.Add(varTriangleBuf[i] + fillVertCount);
            mesh.SetTriangles(remappedVarTrisBuf, submesh: submesh++);
        }
        if (showBorders)
        {
            // List<int> overload avoids the per-regen array copy that ToArray() used to make.
            mesh.SetIndices(lineBuf, MeshTopology.Lines, submesh);
        }

        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Palette-matched fallback variation texture. Base is a very-subtly darker
    /// shade of the ground fill so the patch reads as "same grass"; scattered
    /// tuft pixels (a few darker, one or two lighter) give the eye something
    /// to catch. Sampled through the diamond UV in RebuildMesh, so only the
    /// centered diamond region of the texture actually shows.
    /// </summary>
    private static Texture2D BuildProceduralVariation(Color baseColor)
    {
        int w = IsoProjection.TILE_WIDTH_PIXELS;
        int h = IsoProjection.TILE_HEIGHT_PIXELS;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp,
        };

        Color patch     = new Color(baseColor.r * 0.92f, baseColor.g * 0.92f, baseColor.b * 0.92f, 1f);
        Color tuftDark  = new Color(baseColor.r * 0.72f, baseColor.g * 0.72f, baseColor.b * 0.72f, 1f);
        Color tuftLight = new Color(
            Mathf.Min(1f, baseColor.r * 1.15f),
            Mathf.Min(1f, baseColor.g * 1.15f),
            Mathf.Min(1f, baseColor.b * 1.15f),
            1f);

        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = patch;

        // Fixed tuft positions inside the diamond region so the pattern is
        // consistent across variation tiles.
        int[] tuftXs = { 10, 15, 21,  8, 24, 13, 19 };
        int[] tuftYs = {  6,  4,  9,  8,  7, 10,  5 };
        for (int i = 0; i < tuftXs.Length; i++)
        {
            pixels[tuftYs[i] * w + tuftXs[i]] = (i % 3 == 0) ? tuftLight : tuftDark;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Cheap deterministic hash from tile coords to a uint. Same tile always
    /// returns the same value, independent of camera or frame — so the scatter
    /// stays put as the player walks around.
    /// </summary>
    private static uint TileHash(int worldX, int worldY)
    {
        unchecked
        {
            uint h = (uint)(worldX * 73856093) ^ (uint)(worldY * 19349663);
            h = (h ^ (h >> 16)) * 0x85ebca6b;
            h = (h ^ (h >> 13)) * 0xc2b2ae35;
            h = h ^ (h >> 16);
            return h;
        }
    }
}
