using UnityEngine;

/// <summary>
/// The Natalia-view checkbox described in the Phase 1.5 design brief.
/// Sits above the magnifying-glass search button; checking it snaps the
/// camera to Natalia (follow-camera) mode, unchecking it drops into Free
/// Cam mode. Clicking the minimap also uncheck-flips this automatically,
/// so the toggle always reflects the current camera mode.
///
/// Rendered via IMGUI to match <see cref="MinimapUI"/>. Position is
/// configurable; default lands the toggle in the lower-right where the
/// minimap sits so both are grouped together in the HUD.
/// </summary>
public class NataliaViewToggle : MonoBehaviour
{
    [Header("References (auto-found if left null)")]
    [SerializeField] private CameraController cameraController;

    [Header("Layout")]
    [Tooltip("Distance from the bottom-right corner of the screen, in pixels. Default groups the toggle just above where the minimap sits.")]
    [SerializeField] private Vector2 marginFromBottomRight = new Vector2(12f, 205f);
    [SerializeField] private Vector2 size = new Vector2(180f, 26f);

    [Header("Style")]
    [SerializeField] private int fontSize = 14;
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.55f);

    private Texture2D pixelTex;
    private GUIStyle labelStyle;

    private void OnEnable()
    {
        pixelTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixelTex.SetPixel(0, 0, Color.white);
        pixelTex.filterMode = FilterMode.Point;
        pixelTex.Apply();
    }

    private void OnGUI()
    {
        if (pixelTex == null) return;
        if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController == null) return;

        // Plot Overview owns V for toggle; hide the Natalia-view checkbox
        // while it's active so the HUD stays uncluttered.
        if (cameraController.Mode == CameraMode.PlotOverview) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = fontSize,
                normal   = { textColor = labelColor },
                onNormal = { textColor = labelColor },
                hover    = { textColor = labelColor },
                onHover  = { textColor = labelColor },
                active   = { textColor = labelColor },
                onActive = { textColor = labelColor },
            };
        }

        Rect rect = new Rect(
            Screen.width  - size.x - marginFromBottomRight.x,
            Screen.height - size.y - marginFromBottomRight.y,
            size.x, size.y);

        // Background chip so the toggle reads on any ground colour.
        Color prevGuiColor = GUI.color;
        GUI.color = backgroundColor;
        GUI.DrawTexture(rect, pixelTex);
        GUI.color = prevGuiColor;

        bool isNatalia = cameraController.Mode == CameraMode.Natalia;
        bool nextIsNatalia = GUI.Toggle(
            new Rect(rect.x + 6f, rect.y + 3f, rect.width - 12f, rect.height - 6f),
            isNatalia, " Natalia view", labelStyle);

        if (nextIsNatalia != isNatalia)
        {
            cameraController.SetMode(nextIsNatalia ? CameraMode.Natalia : CameraMode.FreeCam);
        }
    }
}
