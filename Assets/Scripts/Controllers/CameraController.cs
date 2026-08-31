using UnityEngine;

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
    [Tooltip("Half-width of the deadzone rectangle in Unity units. Character can roam within +/- this from camera center without the camera moving.")]
    [SerializeField] private float deadzoneHalfWidthUnits = 4f;
    [Tooltip("Half-height of the deadzone rectangle in Unity units.")]
    [SerializeField] private float deadzoneHalfHeightUnits = 2f;

    private const float CAMERA_Z = -10f;

    /// <summary>Camera position expressed in pre-iso pixel units (Unity units × 16). Kept for UI back-compat.</summary>
    public Vector2 CameraOffset => new Vector2(transform.position.x * 16f, transform.position.y * 16f);

    private void Awake()
    {
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
        if (target == null) return;

        Vector3 targetPos = target.transform.position;
        Vector3 cameraPos = transform.position;

        float dx = targetPos.x - cameraPos.x;
        float dy = targetPos.y - cameraPos.y;

        if (dx >  deadzoneHalfWidthUnits)  cameraPos.x = targetPos.x - deadzoneHalfWidthUnits;
        if (dx < -deadzoneHalfWidthUnits)  cameraPos.x = targetPos.x + deadzoneHalfWidthUnits;
        if (dy >  deadzoneHalfHeightUnits) cameraPos.y = targetPos.y - deadzoneHalfHeightUnits;
        if (dy < -deadzoneHalfHeightUnits) cameraPos.y = targetPos.y + deadzoneHalfHeightUnits;

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
}
