using UnityEngine;

/// <summary>
/// Controls camera movement and tracks camera offset for sector generation.
/// Handles WASD input for infinite scrolling through the mushroom world.
///
/// IMPORTANT SETUP STEPS:
/// 1. In Unity, select Assets/Settings/InputActions.inputactions
/// 2. In Inspector, check "Generate C# Class"
/// 3. Set "Class Name" to "InputActions"
/// 4. Click "Apply"
/// 5. If compile errors occur, use the temporary Legacy Input version below
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 120f; // 120 pixels per second (2 pixels per frame @ 60fps)
    [SerializeField] private float sprintMultiplier = 2.0f; // Speed multiplier when holding Shift

    // Track camera offset in pixel coordinates (not Unity units)
    private Vector2 cameraOffset = Vector2.zero;
    public Vector2 CameraOffset => cameraOffset;

    // Unity Input System
    private InputActions inputActions;
    private UnityEngine.InputSystem.InputAction moveAction;
    private UnityEngine.InputSystem.InputAction sprintAction;

    private void Awake()
    {
        inputActions = new InputActions();
        moveAction = inputActions.Player.Move;
        sprintAction = inputActions.Player.Sprint;
    }

    private void Start()
    {
        // Restore camera position from WorldManager if a world is loaded
        RestoreCameraPosition();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();

        // Save camera position when disabled (scene transition or quit)
        SaveCameraPosition();
    }

    private void OnApplicationQuit()
    {
        // Ensure camera position is saved on quit
        SaveCameraPosition();
    }

    /// <summary>
    /// Restore camera position from the currently loaded world.
    /// </summary>
    private void RestoreCameraPosition()
    {
        if (WorldManager.Instance != null && WorldManager.Instance.HasActiveWorld)
        {
            Vector2 savedPosition = WorldManager.Instance.CurrentWorld.lastCameraPosition;
            SetOffset(savedPosition);
        }
    }

    /// <summary>
    /// Save current camera position to the loaded world.
    /// </summary>
    private void SaveCameraPosition()
    {
        if (WorldManager.Instance != null && WorldManager.Instance.HasActiveWorld)
        {
            WorldManager.Instance.SaveCameraPosition(cameraOffset);
        }
    }

    private void LateUpdate()
    {
        // Read WASD input as Vector2
        Vector2 movement = moveAction.ReadValue<Vector2>();

        // Check if sprint is being held
        bool isSprinting = sprintAction.IsPressed();
        float currentSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

        // Update offset in pixel coordinates
        cameraOffset += movement * currentSpeed * Time.deltaTime;

        // Update camera position (convert pixels to Unity units: 16 pixels = 1 unit)
        // No rounding - let Unity handle sub-pixel rendering for smooth movement
        transform.position = new Vector3(
            cameraOffset.x / 16f,
            cameraOffset.y / 16f,
            -10f // Camera Z position
        );
    }

    /// <summary>
    /// Reset camera to origin (useful for testing).
    /// </summary>
    public void ResetToOrigin()
    {
        cameraOffset = Vector2.zero;
        transform.position = new Vector3(0, 0, -10f);
    }

    /// <summary>
    /// Set camera to specific pixel offset.
    /// </summary>
    public void SetOffset(Vector2 newOffset)
    {
        cameraOffset = newOffset;
        transform.position = new Vector3(
            cameraOffset.x / 16f,
            cameraOffset.y / 16f,
            -10f
        );
    }
}
