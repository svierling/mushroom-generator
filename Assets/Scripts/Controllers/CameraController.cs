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

    private const float CAMERA_Z = -10f;
    private const float SCROLL_STEP_THRESHOLD = 0.5f;

    private Camera cam;
    private float baseOrthoSize;
    private int currentZoomIndex;
    private float scrollAccumulator;

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

        // Number-key jumps: 1/2/3 → index 0/1/2 (0.5x / 1x / 2x)
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) SetZoomIndex(0);
            else if (kb.digit2Key.wasPressedThisFrame) SetZoomIndex(1);
            else if (kb.digit3Key.wasPressedThisFrame) SetZoomIndex(2);
        }

        // Mouse wheel: accumulate small deltas until we cross a threshold, then step.
        // Standard scroll wheels emit ~1 unit per click; the threshold gives one
        // zoom step per click without letting rapid trackpad flicks skip past a level.
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
        if (target == null || cam == null) return;

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
        if (WorldManager.Instance != null && WorldManager.Instance.HasActiveWorld)
        {
            SetOffset(WorldManager.Instance.CurrentWorld.lastCameraPosition);
        }
    }

    private void SaveCameraPosition()
    {
        if (WorldManager.Instance != null && WorldManager.Instance.HasActiveWorld)
        {
            WorldManager.Instance.SaveCameraPosition(CameraOffset);
        }
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
