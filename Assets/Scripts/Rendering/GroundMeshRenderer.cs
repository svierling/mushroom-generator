using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renders the whole ground grid as a single dynamic mesh — one MeshRenderer,
/// two submeshes: filled diamonds for tile bodies, line-topology segments for
/// tile borders. No textures, so there are no pixel-level aliasing artifacts
/// at any zoom level.
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

    [Header("Layering")]
    [Tooltip("Sorting order for the whole ground mesh. Unity's Renderer.sortingOrder is Int16, so use its floor (-32768) — this guarantees the ground stays below any sprite whose sort key hasn't clamped.")]
    [SerializeField] private int sortingOrder = short.MinValue;

    private const float VISIBILITY_MARGIN_UNITS = 2f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private Material fillMat;
    private Material lineMat;

    private int lastMinTileX, lastMaxTileX, lastMinTileY, lastMaxTileY;
    private float lastOrthoSize;
    private bool hasCachedFrame;

    private readonly List<Vector3> vertexBuf   = new List<Vector3>(8192);
    private readonly List<Color>   colorBuf    = new List<Color>(8192);
    private readonly List<int>     triangleBuf = new List<int>(12288);
    private readonly List<int>     lineBuf     = new List<int>(16384);

    private void Awake()
    {
        meshFilter   = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh { name = "GroundMesh" };
        mesh.MarkDynamic();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.subMeshCount = 2;
        meshFilter.sharedMesh = mesh;

        // Sprites/Default multiplies vertex color × texture. With no texture set
        // the shader falls through to vertex color, so we don't need a shader
        // switch to get solid colored geometry.
        Shader shader = Shader.Find("Sprites/Default");
        fillMat = new Material(shader);
        lineMat = new Material(shader);
        meshRenderer.sharedMaterials = new[] { fillMat, lineMat };
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
        colorBuf.Clear();
        triangleBuf.Clear();
        lineBuf.Clear();

        float halfW = IsoProjection.TILE_WIDTH_PIXELS  * 0.5f / IsoProjection.PIXELS_PER_UNIT; // 1.0
        float halfH = IsoProjection.TILE_HEIGHT_PIXELS * 0.5f / IsoProjection.PIXELS_PER_UNIT; // 0.5

        // Each tile emits 4 verts arranged as top/right/bottom/left of the
        // diamond, two triangles for the fill, and four line-segment indices
        // tracing the outline. Adjacent tiles duplicate shared edges (drawn
        // twice), which is cheap and avoids the bookkeeping of a shared vertex
        // pool.
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

                Vector3 top    = new Vector3(center.x,         center.y + halfH, 0f);
                Vector3 right  = new Vector3(center.x + halfW, center.y,         0f);
                Vector3 bottom = new Vector3(center.x,         center.y - halfH, 0f);
                Vector3 left   = new Vector3(center.x - halfW, center.y,         0f);

                // Verts are white; each submesh's material tint drives its color.
                vertexBuf.Add(top);    colorBuf.Add(Color.white);
                vertexBuf.Add(right);  colorBuf.Add(Color.white);
                vertexBuf.Add(bottom); colorBuf.Add(Color.white);
                vertexBuf.Add(left);   colorBuf.Add(Color.white);

                // Fill: two triangles covering the diamond.
                triangleBuf.Add(v0);     triangleBuf.Add(v0 + 1); triangleBuf.Add(v0 + 2);
                triangleBuf.Add(v0);     triangleBuf.Add(v0 + 2); triangleBuf.Add(v0 + 3);

                // Border: four line segments tracing the outline.
                lineBuf.Add(v0);     lineBuf.Add(v0 + 1);
                lineBuf.Add(v0 + 1); lineBuf.Add(v0 + 2);
                lineBuf.Add(v0 + 2); lineBuf.Add(v0 + 3);
                lineBuf.Add(v0 + 3); lineBuf.Add(v0);
            }
        }

        // Sprites/Default multiplies vertex × material color; verts are white
        // so each submesh renders at its material's tint.
        fillMat.color = fillColor;
        lineMat.color = borderColor;

        mesh.Clear();
        mesh.SetVertices(vertexBuf);
        mesh.SetColors(colorBuf);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangleBuf, submesh: 0);
        mesh.SetIndices(lineBuf.ToArray(), MeshTopology.Lines, submesh: 1);
        mesh.RecalculateBounds();
    }
}
