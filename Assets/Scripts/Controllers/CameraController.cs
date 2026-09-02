using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Follow-camera with a deadzone window. The player character can roam within
/// a central rectangle on screen without the camera moving; only when the
/// character pushes against the edge does the camera scroll to keep them
/// inside the window.
///
/// Backward-compat: <see cref="CameraOffset"/> and <see cref="SetOffset"/> keep
/// the same pixel-offset semantics they had pre-iso, so the existing search /
/// coordinate-tracker UI keeps working. SetOffset now teleports the player
/// (via <see cref="PlayerController.TeleportToTile"/>), and the camera catches
/// up on the next frame's deadzone follow.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private PlayerController target;
    public PlayerController Target => target;

    [Header("Deadzone")]
    [Tooltip("Deadzone half-width as a fraction of the camera's on-screen half-width. Character roams this fraction of the screen (both sides) before the camera scrolls. Scales with zoom so it feels consistent at 0.5x/1x/2x.")]
    [Range(0f, 0.9f)]
    [SerializeField] private float deadzoneHalfWidthScreenFraction = 0.33f;
    [Tooltip("Deadzone half-height as a fraction of the camera's on-screen half-height.")]
    [Range(0f, 0.9f)]
    [SerializeField] private float deadzoneHalfHeightScreenFraction = 0.33f;

    [Header("Zoom")]
    [Tooltip("Zoom multipliers in ascending order. Index 1 is the reference '1x' level; the camera's initial orthographicSize is treated as the 1x size.")]
    [SerializeField] private float[] zoomLevels = { 0.5f, 1f, 2f };
    [Tooltip("Which zoom level the camera boots up at. 0=0.5x, 1=1x, 2=2x.")]
    [SerializeField] private int defaultZoomIndex = 1;

    [Header("Free Cam")]
    [Tooltip("Camera panning speed in Unity units per second when Free Cam mode is active.")]
    [SerializeField] private float freeCamSpeedUnitsPerSecond = 10f;
    [Tooltip("Sprint multiplier for Free Cam movement.")]
    [SerializeField] private float freeCamSprintMultiplier = 2.5f;

    [Header("Plot Overview")]
    [Tooltip("Padding around the plot when auto-fitting the Plot Overview view. 0 = plot bounds exactly at screen edges; 0.05 = 5% padding around it.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float plotOverviewPadding = 0.05f;

    private const float CAMERA_Z = -10f;
    private const float SCROLL_STEP_THRESHOLD = 0.5f;

    private Camera cam;
    private float baseOrthoSize;
    private int currentZoomIndex;
    private float scrollAccumulator;

    // Camera mode state. Natalia is the default and matches shipped behaviour.
    private CameraMode currentMode = CameraMode.Natalia;
    // Where to restore when leaving PlotOverview. Never gets set to PlotOverview
    // so successive V presses toggle between the fitted view and whichever
    // mode the player was actually using.
    private CameraMode preOverviewMode = CameraMode.Natalia;
    // Zoom to restore when leaving PlotOverview — that mode has its own
    // auto-fitted orthographicSize independent of the zoom preset list.
    private int preOverviewZoomIndex;

    /// <summary>Which camera mode is currently active.</summary>
    public CameraMode Mode => currentMode;

    /// <summary>Emitted when <see cref="Mode"/> changes; UI (Natalia toggle, minimap) listens.</summary>
    public event System.Action<CameraMode> OnModeChanged;

    /// <summary>Camera position expressed in pre-iso pixel units (Unity units × 16). Kept for UI back-compat.</summary>
    public Vector2 CameraOffset => new Vector2(transform.position.x * 16f, transform.position.y * 16f);

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        // Whatever orthographicSize the Main Camera is configured with in the
        // scene becomes the "1x" reference. All zoom levels are multipliers
        // against this base.
        baseOrthoSize = cam != null ? cam.orthographicSize : 5f;
        currentZoomIndex = Mathf.Clamp(defaultZoomIndex, 0, zoomLevels.Length - 1);

        if (target == null)
        {
            target = FindFirstObjectByType<PlayerController>();
            if (target == null)
            {
                Debug.LogError("CameraController: no PlayerController target assigned and none found in the scene.");
            }
        }
    }

    private void Start()
    {
        RestoreCameraPosition();
        RestoreZoom();
        ApplyZoom();
    }

    private void Update()
    {
        if (cam == null) return;

        Keyboard kb = Keyboard.current;

        // V toggles PlotOverview on / off — regardless of the mode we're in.
        if (kb != null && kb.vKey.wasPressedThisFrame)
        {
            if (currentMode == CameraMode.PlotOverview) ExitPlotOverview();
            else EnterPlotOverview();
        }

        // Zoom controls (number keys + mouse wheel) are disabled in Plot
        // Overview since that mode has its own auto-fitted zoom.
        if (currentMode != CameraMode.PlotOverview)
        {
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) SetZoomIndex(0);
                else if (kb.digit2Key.wasPressedThisFrame) SetZoomIndex(1);
                else if (kb.digit3Key.wasPressedThisFrame) SetZoomIndex(2);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.y.ReadValue();
                scrollAccumulator += scroll;
                if (scrollAccumulator >  SCROLL_STEP_THRESHOLD)
                {
                    SetZoomIndex(currentZoomIndex + 1);
                    scrollAccumulator = 0f;
                }
                else if (scrollAccumulator < -SCROLL_STEP_THRESHOLD)
                {
                    SetZoomIndex(currentZoomIndex - 1);
                    scrollAccumulator = 0f;
                }
            }
        }

        // WASD moves the camera in Free Cam mode. Natalia mode leaves this
        // input alone so PlayerController can consume it for the character.
        if (currentMode == CameraMode.FreeCam && kb != null)
        {
            float dx = 0f, dy = 0f;
            if (kb.wKey.isPressed) dy += 1f;
            if (kb.sKey.isPressed) dy -= 1f;
            if (kb.dKey.isPressed) dx += 1f;
            if (kb.aKey.isPressed) dx -= 1f;
            if (dx != 0f || dy != 0f)
            {
                float speed = freeCamSpeedUnitsPerSecond;
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) speed *= freeCamSprintMultiplier;
                Vector3 pos = transform.position;
                pos.x += dx * speed * Time.deltaTime;
                pos.y += dy * speed * Time.deltaTime;
                transform.position = new Vector3(pos.x, pos.y, CAMERA_Z);
            }
        }
    }

    private void SetZoomIndex(int index)
    {
        int clamped = Mathf.Clamp(index, 0, zoomLevels.Length - 1);
        if (clamped == currentZoomIndex) return;
        currentZoomIndex = clamped;
        ApplyZoom();
        SaveZoom();
    }

    private void ApplyZoom()
    {
        if (cam == null || zoomLevels == null || zoomLevels.Length == 0) return;
        float zoom = zoomLevels[currentZoomIndex];
        // Zoom > 1 = closer (smaller view), zoom < 1 = further (larger view).
        cam.orthographicSize = baseOrthoSize / zoom;
    }

    private void OnDisable()
    {
        SaveCameraPosition();
    }

    private void OnApplicationQuit()
    {
        SaveCameraPosition();
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        // Deadzone follow only runs in Natalia mode. Free Cam and Plot
        // Overview position the camera themselves.
        if (currentMode != CameraMode.Natalia || target == null) return;

        // Deadzone scales with camera zoom: the on-screen area the character
        // roams in stays consistent at 0.5x / 1x / 2x zoom. At bigger orthoSize
        // (zoomed out) the deadzone in world units grows; at smaller orthoSize
        // (zoomed in) it shrinks.
        float halfHeightUnits = cam.orthographicSize;
        float halfWidthUnits  = halfHeightUnits * cam.aspect;
        float dzHalfWidth  = halfWidthUnits  * deadzoneHalfWidthScreenFraction;
        float dzHalfHeight = halfHeightUnits * deadzoneHalfHeightScreenFraction;

        Vector3 targetPos = target.transform.position;
        Vector3 cameraPos = transform.position;

        float dx = targetPos.x - cameraPos.x;
        float dy = targetPos.y - cameraPos.y;

        if (dx >  dzHalfWidth)  cameraPos.x = targetPos.x - dzHalfWidth;
        if (dx < -dzHalfWidth)  cameraPos.x = targetPos.x + dzHalfWidth;
        if (dy >  dzHalfHeight) cameraPos.y = targetPos.y - dzHalfHeight;
        if (dy < -dzHalfHeight) cameraPos.y = targetPos.y + dzHalfHeight;

        transform.position = new Vector3(cameraPos.x, cameraPos.y, CAMERA_Z);
    }

    /// <summary>
    /// Switch camera mode. Called by the Natalia-view toggle (Natalia ↔ FreeCam)
    /// and by the minimap (FreeCam when the player clicks somewhere on it).
    /// PlotOverview is entered / exited via V and the dedicated methods below.
    /// </summary>
    public void SetMode(CameraMode mode)
    {
        if (mode == currentMode) return;
        // Coming out of PlotOverview means restoring the fitted zoom back to
        // the previous preset before setting the new mode.
        if (currentMode == CameraMode.PlotOverview)
        {
            currentZoomIndex = preOverviewZoomIndex;
            ApplyZoom();
        }
        currentMode = mode;
        OnModeChanged?.Invoke(mode);
    }

    /// <summary>
    /// Teleport the free camera to the given world-tile position, switching
    /// into Free Cam mode as a side effect. Called by the minimap on click.
    /// The character stays put.
    /// </summary>
    public void JumpFreeCamTo(float worldTileX, float worldTileY)
    {
        Vector3 unityPos = IsoProjection.WorldToUnity(worldTileX, worldTileY);
        transform.position = new Vector3(unityPos.x, unityPos.y, CAMERA_Z);
        SetMode(CameraMode.FreeCam);
    }

    private void EnterPlotOverview()
    {
        preOverviewMode      = currentMode == CameraMode.PlotOverview ? preOverviewMode : currentMode;
        preOverviewZoomIndex = currentZoomIndex;
        currentMode = CameraMode.PlotOverview;
        FitCameraToPlot();
        OnModeChanged?.Invoke(currentMode);
    }

    private void ExitPlotOverview() => SetMode(preOverviewMode);

    /// <summary>
    /// Position the camera at the plot centre and choose an orthographicSize
    /// that fits the whole plot inside the viewport with a bit of padding.
    /// Bounds are ±(plotSideTiles/2) in world coords; iso-projected, that's a
    /// diamond in Unity space that we fit conservatively (worst-case width).
    /// </summary>
    private void FitCameraToPlot()
    {
        transform.position = new Vector3(0f, 0f, CAMERA_Z);

        int half = WorldBounds.HalfExtent;
        // Iso projection: worst-case Unity extent for a square plot centred
        // on origin is width = 2*half (world +X against world +Y across the
        // full diagonal) and height = half (top-to-bottom of the diamond).
        // Height is smaller than width, so vertical fit typically wins.
        float aspect = (cam != null && cam.aspect > 0f) ? cam.aspect : (16f / 9f);
        float halfWidthUnits  = half; // (worldX - worldY) span / 2 = half
        float halfHeightUnits = half * 0.5f; // (worldX + worldY) span / 4 = half/2

        // orthographicSize is half the camera's vertical extent; ensure both
        // dimensions fit by picking the larger of the two demands.
        float sizeForHeight = halfHeightUnits;
        float sizeForWidth  = halfWidthUnits / Mathf.Max(aspect, 0.0001f);
        float fitted = Mathf.Max(sizeForHeight, sizeForWidth);
        fitted *= (1f + plotOverviewPadding);
        if (cam != null) cam.orthographicSize = fitted;
    }

    /// <summary>
    /// Teleport to a specific pixel offset — used by the coordinate search UI.
    /// Converts the target Unity position back to a tile, teleports the player
    /// there, and snaps the camera on top so the deadzone follow doesn't have
    /// to catch up.
    /// </summary>
    public void SetOffset(Vector2 pixelOffset)
    {
        float unityX = pixelOffset.x / 16f;
        float unityY = pixelOffset.y / 16f;

        if (target != null)
        {
            Vector2 tile = IsoProjection.UnityToWorld(unityX, unityY);
            target.TeleportToTile(tile.x, tile.y);
        }

        transform.position = new Vector3(unityX, unityY, CAMERA_Z);
    }

    public void ResetToOrigin()
    {
        SetOffset(Vector2.zero);
    }

    private void RestoreCameraPosition()
    {
        if (WorldManager.Instance == null || !WorldManager.Instance.HasActiveWorld) return;

        WorldSaveData world = WorldManager.Instance.CurrentWorld;

        // Prefer the character tile (authoritative). Fall back to the legacy
        // pixel-offset field for worlds saved before iso. Both defaulting to
        // (0, 0) means "start at origin", which is correct on fresh worlds too.
        if (world.lastCharacterTile != Vector2.zero && target != null)
        {
            target.TeleportToTile(world.lastCharacterTile.x, world.lastCharacterTile.y);
            SnapCameraToTarget();
        }
        else
        {
            SetOffset(world.lastCameraPosition);
        }
    }

    private void SaveCameraPosition()
    {
        if (WorldManager.Instance == null || !WorldManager.Instance.HasActiveWorld) return;

        WorldSaveData world = WorldManager.Instance.CurrentWorld;

        // Character tile is the authoritative source of truth on load; the
        // legacy pixel-offset field is still populated for backward compat.
        // Setting the field directly means both writes flush in one disk hit.
        if (target != null)
        {
            world.lastCharacterTile = new Vector2(target.WorldTileX, target.WorldTileY);
        }
        WorldManager.Instance.SaveCameraPosition(CameraOffset);
    }

    private void SnapCameraToTarget()
    {
        if (target == null) return;
        Vector3 t = target.transform.position;
        transform.position = new Vector3(t.x, t.y, CAMERA_Z);
    }

    private void RestoreZoom()
    {
        if (WorldManager.Instance != null && WorldManager.Instance.HasActiveWorld)
        {
            int saved = WorldManager.Instance.CurrentWorld.lastZoomIndex;
            // Guard against out-of-range saves (older worlds default to 0, which
            // happens to be the 0.5x level — we still want 1x as the sensible
            // default for worlds that predate zoom persistence).
            if (saved < 0 || saved >= zoomLevels.Length) saved = defaultZoomIndex;
            currentZoomIndex = saved;
        }
    }

    private void SaveZoom()
    {
        if (WorldManager.Instance != null && WorldManager.Instance.HasActiveWorld)
        {
            WorldManager.Instance.SaveZoomIndex(currentZoomIndex);
        }
    }
}
