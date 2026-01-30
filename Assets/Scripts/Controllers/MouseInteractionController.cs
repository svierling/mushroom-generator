using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Handles mouse position tracking, hover detection, and click events.
/// Converts screen coordinates to sector coordinates matching C++ implementation.
/// Implements hover highlighting and click detection from MushroomGenerator.cpp lines 198-229.
/// Clicking outside mushrooms and UI closes the info window.
/// </summary>
public class MouseInteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private HighlightRectangle highlightRect;
    [SerializeField] private SelectionManager selectionManager;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private const int PIXELS_PER_UNIT = 16;
    private const int SECTOR_SIZE_PIXELS = 16;

    // Mushroom sprite dimensions (from MushroomRed.png: 46x53 pixels)
    private const float SPRITE_WIDTH_PIXELS = 46f;
    private const float SPRITE_HEIGHT_PIXELS = 53f;

    private InputActions inputActions;
    private UnityEngine.InputSystem.InputAction clickAction;

    private Vector2Int currentHoveredSector = Vector2Int.zero;
    private bool isHoveringMushroom = false;

    private void Awake()
    {
        inputActions = new InputActions();
        clickAction = inputActions.Player.Click;
        clickAction.performed += OnClick;

        // Verify references
        if (mainCamera == null)
        {
            Debug.LogError("MouseInteractionController: Main Camera reference is missing!");
        }
        if (highlightRect == null)
        {
            Debug.LogError("MouseInteractionController: Highlight Rectangle reference is missing!");
        }
        if (selectionManager == null)
        {
            Debug.LogError("MouseInteractionController: Selection Manager reference is missing!");
        }
    }

    private void OnEnable()
    {
        clickAction.Enable();
    }

    private void OnDisable()
    {
        clickAction.Disable();
    }

    private void Update()
    {
        UpdateMousePosition();
    }

    /// <summary>
    /// Update mouse position and hover state every frame.
    /// Matches C++ logic from lines 198-215.
    /// </summary>
    private void UpdateMousePosition()
    {
        // Safety check - ensure mouse is available
        if (Mouse.current == null)
        {
            return;
        }

        // Get mouse screen position
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // Convert to world position
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f)
        );

        // Check nearby sectors for mushrooms (3x3 grid around mouse position)
        // This ensures we detect mushrooms whose sprites overlap the mouse position
        isHoveringMushroom = false;
        currentHoveredSector = Vector2Int.zero;

        // Calculate sprite half-dimensions in world units
        float spriteHalfWidth = (SPRITE_WIDTH_PIXELS / PIXELS_PER_UNIT) / 2f;  // 1.4375
        float spriteHalfHeight = (SPRITE_HEIGHT_PIXELS / PIXELS_PER_UNIT) / 2f; // 1.65625

        // Check a 3x3 grid of sectors around the mouse position
        int centerSectorX = Mathf.FloorToInt(worldPos.x);
        int centerSectorY = Mathf.FloorToInt(worldPos.y);

        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                int checkSectorX = centerSectorX + offsetX;
                int checkSectorY = centerSectorY + offsetY;

                // Check if mushroom exists at this sector
                MushroomData data = MushroomData.Generate(
                    (uint)checkSectorX,
                    (uint)checkSectorY
                );

                if (data.exists)
                {
                    // Calculate mushroom center position
                    // Matches MushroomGenerator: (sector * 16 - 8) / 16
                    float mushroomCenterX = (checkSectorX * SECTOR_SIZE_PIXELS - 8f) / PIXELS_PER_UNIT;
                    float mushroomCenterY = (checkSectorY * SECTOR_SIZE_PIXELS - 8f) / PIXELS_PER_UNIT;

                    // Check if mouse is within mushroom sprite bounds
                    float deltaX = Mathf.Abs(worldPos.x - mushroomCenterX);
                    float deltaY = Mathf.Abs(worldPos.y - mushroomCenterY);

                    if (deltaX < spriteHalfWidth && deltaY < spriteHalfHeight)
                    {
                        // Mouse is hovering over this mushroom!
                        isHoveringMushroom = true;
                        currentHoveredSector = new Vector2Int(checkSectorX, checkSectorY);
                        goto FoundMushroom; // Exit both loops
                    }
                }
            }
        }

        FoundMushroom:; // Label for breaking out of nested loops

        // Debug output
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Mouse sector: ({currentHoveredSector.x}, {currentHoveredSector.y}) | Mushroom exists: {isHoveringMushroom}");
        }

        // Update highlight rectangle
        if (isHoveringMushroom)
        {
            // Calculate world position for highlight (matching C++ offset: screen_sector * 16 - 8)
            Vector3 highlightWorldPos = new Vector3(
                (currentHoveredSector.x * SECTOR_SIZE_PIXELS - 8) / (float)PIXELS_PER_UNIT,
                (currentHoveredSector.y * SECTOR_SIZE_PIXELS - 8) / (float)PIXELS_PER_UNIT,
                0f
            );
            highlightRect.Show(highlightWorldPos);
        }
        else
        {
            highlightRect.Hide();
        }
    }

    /// <summary>
    /// Handle left mouse button click.
    /// Matches C++ click detection from lines 219-227.
    /// Also handles deselection when clicking outside mushrooms and UI.
    /// </summary>
    private void OnClick(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        Debug.Log($"Click detected at sector ({currentHoveredSector.x}, {currentHoveredSector.y}) | Hovering mushroom: {isHoveringMushroom}");

        if (isHoveringMushroom)
        {
            // Select the hovered mushroom
            selectionManager.SelectMushroom(
                (uint)currentHoveredSector.x,
                (uint)currentHoveredSector.y
            );
        }
        else
        {
            // Check if click is over UI (info window)
            // If not over UI and not over mushroom, deselect
            if (!IsPointerOverUIElement())
            {
                selectionManager.Deselect();
            }
        }
    }

    /// <summary>
    /// Check if the mouse pointer is currently over a UI element.
    /// Returns true if hovering over UI (like the info window).
    /// </summary>
    private bool IsPointerOverUIElement()
    {
        // Check if there's an EventSystem (required for UI raycasting)
        if (EventSystem.current == null)
        {
            return false;
        }

        // Create pointer event data for the current mouse position
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        // Raycast to check for UI elements
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }
}
