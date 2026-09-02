using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Handles mouse position tracking, hover detection, and click events.
/// Converts screen coordinates to tile coordinates matching C++ implementation
/// (the C++ port called tiles "sectors").
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

    // Mushroom sprite dimensions (60x60 with a 5-pixel bottom-transparent
    // padding — the .meta pivot is set to the visible base at pixel y=5).
    private const float SPRITE_WIDTH_PIXELS  = 60f;
    private const float SPRITE_HEIGHT_PIXELS = 60f;
    private const float SPRITE_PIVOT_Y_PIXELS = 5f;
    private const float SPRITE_HALF_WIDTH_UNITS  = SPRITE_WIDTH_PIXELS  * 0.5f / PIXELS_PER_UNIT;
    private const float SPRITE_HALF_HEIGHT_UNITS = SPRITE_HEIGHT_PIXELS * 0.5f / PIXELS_PER_UNIT;
    // Sprite pivot sits at the tile point (see MushroomInstance.Configure).
    // The sprite CENTER for hit-testing is therefore (halfHeight - pivotY)
    // above the tile.
    private const float MUSHROOM_BASE_OFFSET_UNITS = (SPRITE_HEIGHT_PIXELS * 0.5f - SPRITE_PIVOT_Y_PIXELS) / PIXELS_PER_UNIT;

    // Maximum terrain height we'll ever try to pick against. Flat today, but
    // the walk-down loop is already the right shape for Phase 0.75.
    private const int MAX_PICK_HEIGHT = 0;
    // Search radius (in tiles) around the cursor's back-projected tile. Needs
    // to cover mushrooms whose base is a few tiles behind the cursor but whose
    // tall sprite reaches forward into the cursor's screen area.
    private const int MUSHROOM_SEARCH_RADIUS = 3;

    private InputActions inputActions;
    private UnityEngine.InputSystem.InputAction clickAction;

    private Vector2Int currentHoveredTile = Vector2Int.zero;
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

        isHoveringMushroom = false;
        currentHoveredTile = Vector2Int.zero;

        // Height walk-down: try the tallest possible height first, project the
        // cursor down to that height's tile, check if terrain there actually is
        // that tall (or taller). Flat terrain today → one iteration.
        Vector2 pickedTileF = IsoProjection.UnityToWorld(worldPos.x, worldPos.y, 0);
        for (int h = MAX_PICK_HEIGHT; h >= 0; h--)
        {
            Vector2 candidate = IsoProjection.UnityToWorld(worldPos.x, worldPos.y, h);
            int cx = Mathf.FloorToInt(candidate.x);
            int cy = Mathf.FloorToInt(candidate.y);
            TerrainSample s = TerrainService.SampleAt(cx, cy);
            if (s.height >= h)
            {
                pickedTileF = candidate;
                break;
            }
        }

        int centerTileX = Mathf.FloorToInt(pickedTileF.x);
        int centerTileY = Mathf.FloorToInt(pickedTileF.y);

        // Scan a small tile neighborhood — a mushroom whose base is on a tile
        // behind the cursor can still overlap the cursor because its sprite
        // extends upward on screen.
        for (int offsetX = -MUSHROOM_SEARCH_RADIUS; offsetX <= MUSHROOM_SEARCH_RADIUS; offsetX++)
        {
            for (int offsetY = -MUSHROOM_SEARCH_RADIUS; offsetY <= MUSHROOM_SEARCH_RADIUS; offsetY++)
            {
                int checkTileX = centerTileX + offsetX;
                int checkTileY = centerTileY + offsetY;

                MushroomData data = MushroomData.Generate((uint)checkTileX, (uint)checkTileY);
                if (!data.exists) continue;

                TerrainSample tileSample = TerrainService.SampleAt(checkTileX, checkTileY);
                Vector3 spriteCenter = IsoProjection.WorldToUnity(checkTileX, checkTileY, tileSample.height)
                                     + new Vector3(0f, MUSHROOM_BASE_OFFSET_UNITS, 0f);

                if (Mathf.Abs(worldPos.x - spriteCenter.x) < SPRITE_HALF_WIDTH_UNITS &&
                    Mathf.Abs(worldPos.y - spriteCenter.y) < SPRITE_HALF_HEIGHT_UNITS)
                {
                    isHoveringMushroom = true;
                    currentHoveredTile = new Vector2Int(checkTileX, checkTileY);
                    goto FoundMushroom;
                }
            }
        }

        FoundMushroom:; // Label for breaking out of nested loops

        // Debug output
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Mouse tile: ({currentHoveredTile.x}, {currentHoveredTile.y}) | Mushroom exists: {isHoveringMushroom}");
        }

        // Update highlight rectangle — position it at the mushroom's iso sprite
        // center so the yellow border frames the sprite regardless of zoom.
        if (isHoveringMushroom)
        {
            TerrainSample hoverSample = TerrainService.SampleAt(currentHoveredTile.x, currentHoveredTile.y);
            Vector3 highlightWorldPos = IsoProjection.WorldToUnity(currentHoveredTile.x, currentHoveredTile.y, hoverSample.height)
                                      + new Vector3(0f, MUSHROOM_BASE_OFFSET_UNITS, 0f);
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
        Debug.Log($"Click detected at tile ({currentHoveredTile.x}, {currentHoveredTile.y}) | Hovering mushroom: {isHoveringMushroom}");

        if (isHoveringMushroom)
        {
            // Select the hovered mushroom
            selectionManager.SelectMushroom(
                (uint)currentHoveredTile.x,
                (uint)currentHoveredTile.y
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
