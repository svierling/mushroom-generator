using UnityEngine;

/// <summary>
/// Minimal RCT-style minimap: plot outline + Natalia dot + camera viewport
/// rectangle. Clicking anywhere on the minimap teleports the camera there
/// via <see cref="CameraController.JumpFreeCamTo"/>, which auto-switches to
/// Free Cam mode. Rendered via IMGUI so no scene wiring is needed — attach
/// this component to any GameObject in MainScene and it draws immediately.
///
/// Deferred to later PRs: biome color patches, terrain contours, mushroom
/// density heatmap — waiting on Phase 5 / Phase 0.75 layers to exist.
/// </summary>
public class MinimapUI : MonoBehaviour
{
    [Header("References (auto-found if left null)")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private PlayerController player;
    [SerializeField] private Camera mainCamera;

    [Header("Layout")]
    [Tooltip("Minimap pixel size on screen.")]
    [SerializeField] private Vector2 minimapSize = new Vector2(180f, 180f);
    [Tooltip("Distance from the bottom-right corner of the screen, in pixels.")]
    [SerializeField] private Vector2 minimapMargin = new Vector2(12f, 12f);

    [Header("Colours")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color outlineColor    = new Color(0.60f, 0.90f, 0.60f, 1f);
    [SerializeField] private Color playerDotColor  = new Color(1f, 0.85f, 0.30f, 1f);
    [SerializeField] private Color viewportColor   = new Color(1f, 1f, 1f, 0.75f);
    [SerializeField] private float outlineThickness = 2f;
    [SerializeField] private float viewportThickness = 1f;
    [SerializeField] private float playerDotSize = 6f;

    private Texture2D pixelTex;

    private void OnEnable()
    {
        // Single-pixel white texture — GUI.DrawTexture tinted with GUI.color
        // gives us solid-colour quads without any shader or material.
        pixelTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixelTex.SetPixel(0, 0, Color.white);
        pixelTex.filterMode = FilterMode.Point;
        pixelTex.Apply();
    }

    private void OnGUI()
    {
        if (pixelTex == null) return;
        if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
        if (player == null)           player           = FindFirstObjectByType<PlayerController>();
        if (mainCamera == null)       mainCamera       = Camera.main;
        if (cameraController == null || player == null || mainCamera == null) return;

        Rect minimapRect = new Rect(
            Screen.width  - minimapSize.x - minimapMargin.x,
            Screen.height - minimapSize.y - minimapMargin.y,
            minimapSize.x, minimapSize.y);

        DrawFilledRect(minimapRect, backgroundColor);
        DrawRectOutline(minimapRect, outlineColor, outlineThickness);

        int halfExtent = WorldBounds.HalfExtent;
        if (halfExtent <= 0) return;

        // Camera-viewport rectangle: back-project the camera's on-screen
        // corners to world-tile coords, then take the AABB. Approximation
        // (real visible area is a diamond) but plenty for a locator.
        Vector3 camPos = mainCamera.transform.position;
        float halfW = mainCamera.orthographicSize * mainCamera.aspect;
        float halfH = mainCamera.orthographicSize;
        Vector2 tl = IsoProjection.UnityToWorld(camPos.x - halfW, camPos.y + halfH);
        Vector2 tr = IsoProjection.UnityToWorld(camPos.x + halfW, camPos.y + halfH);
        Vector2 bl = IsoProjection.UnityToWorld(camPos.x - halfW, camPos.y - halfH);
        Vector2 br = IsoProjection.UnityToWorld(camPos.x + halfW, camPos.y - halfH);
        float vpMinX = Mathf.Min(Mathf.Min(tl.x, tr.x), Mathf.Min(bl.x, br.x));
        float vpMaxX = Mathf.Max(Mathf.Max(tl.x, tr.x), Mathf.Max(bl.x, br.x));
        float vpMinY = Mathf.Min(Mathf.Min(tl.y, tr.y), Mathf.Min(bl.y, br.y));
        float vpMaxY = Mathf.Max(Mathf.Max(tl.y, tr.y), Mathf.Max(bl.y, br.y));
        Vector2 vpTL = WorldToMinimap(vpMinX, vpMaxY, halfExtent, minimapRect);
        Vector2 vpBR = WorldToMinimap(vpMaxX, vpMinY, halfExtent, minimapRect);
        Rect viewportRect = Rect.MinMaxRect(
            Mathf.Min(vpTL.x, vpBR.x), Mathf.Min(vpTL.y, vpBR.y),
            Mathf.Max(vpTL.x, vpBR.x), Mathf.Max(vpTL.y, vpBR.y));
        DrawRectOutline(viewportRect, viewportColor, viewportThickness);

        // Natalia dot
        Vector2 playerPt = WorldToMinimap(player.WorldTileX, player.WorldTileY, halfExtent, minimapRect);
        DrawFilledRect(new Rect(playerPt.x - playerDotSize * 0.5f,
                                playerPt.y - playerDotSize * 0.5f,
                                playerDotSize, playerDotSize),
                       playerDotColor);

        // Click-to-navigate. Convert the click's minimap-local (u, v) back to
        // world tile coords and hand off to the camera controller.
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && minimapRect.Contains(e.mousePosition))
        {
            float u = (e.mousePosition.x - minimapRect.x) / minimapRect.width;
            float v = 1f - (e.mousePosition.y - minimapRect.y) / minimapRect.height; // OnGUI Y is down
            float worldX = (u * 2f - 1f) * halfExtent;
            float worldY = (v * 2f - 1f) * halfExtent;
            cameraController.JumpFreeCamTo(worldX, worldY);
            e.Use();
        }
    }

    private static Vector2 WorldToMinimap(float worldX, float worldY, int halfExtent, Rect rect)
    {
        float u = (worldX + halfExtent) / (2f * halfExtent);
        float v = (worldY + halfExtent) / (2f * halfExtent);
        // OnGUI's Y axis points down, so world +Y maps to lower minimap Y.
        return new Vector2(rect.x + u * rect.width,
                           rect.y + (1f - v) * rect.height);
    }

    private void DrawFilledRect(Rect rect, Color color)
    {
        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixelTex);
        GUI.color = prev;
    }

    private void DrawRectOutline(Rect rect, Color color, float thickness)
    {
        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), pixelTex);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), pixelTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), pixelTex);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), pixelTex);
        GUI.color = prev;
    }
}
