using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders yellow highlight rectangle for hovered mushrooms.
/// Displays a border-only outline matching C++ DrawRect call (line 213).
/// Positions the highlight at sector * 16 - 8 to match sprite centering.
/// </summary>
public class HighlightRectangle : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float borderThickness = 2f;

    private RectTransform rectTransform;
    private Camera mainCamera;
    private Canvas canvas;

    // Border rectangles (top, bottom, left, right)
    private GameObject topBorder, bottomBorder, leftBorder, rightBorder;

    // Highlight dimensions in world space that encompass the mushroom sprite nicely
    // Final size: 64.4% of original (88 * 0.644 = 56.7, 120 * 0.644 = 77.3)
    // while scaling correctly with the window
    private const float HIGHLIGHT_WIDTH_WORLD_PIXELS = 56.7f;
    private const float HIGHLIGHT_HEIGHT_WORLD_PIXELS = 77.3f;
    private const int PIXELS_PER_UNIT = 16;

    private void Awake()
    {
        mainCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        // Remove or disable any Image component on parent (we only want the border children)
        Image parentImage = GetComponent<Image>();
        if (parentImage != null)
        {
            Destroy(parentImage);
        }

        // Remove any Outline component as well
        Outline parentOutline = GetComponent<Outline>();
        if (parentOutline != null)
        {
            Destroy(parentOutline);
        }

        // Create border rectangles as children
        // Size will be calculated dynamically in Show() based on camera zoom
        CreateBorders();

        Hide();
    }

    /// <summary>
    /// Create 4 Image objects to form the border outline.
    /// </summary>
    private void CreateBorders()
    {
        // Top border (horizontal line at top)
        topBorder = CreateBorderRect("TopBorder");
        var topRT = topBorder.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0, 1);
        topRT.anchorMax = new Vector2(1, 1);
        topRT.pivot = new Vector2(0.5f, 1);
        topRT.anchoredPosition = Vector2.zero;
        topRT.sizeDelta = new Vector2(0, borderThickness);

        // Bottom border (horizontal line at bottom)
        bottomBorder = CreateBorderRect("BottomBorder");
        var bottomRT = bottomBorder.GetComponent<RectTransform>();
        bottomRT.anchorMin = new Vector2(0, 0);
        bottomRT.anchorMax = new Vector2(1, 0);
        bottomRT.pivot = new Vector2(0.5f, 0);
        bottomRT.anchoredPosition = Vector2.zero;
        bottomRT.sizeDelta = new Vector2(0, borderThickness);

        // Left border (vertical line at left)
        leftBorder = CreateBorderRect("LeftBorder");
        var leftRT = leftBorder.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 0);
        leftRT.anchorMax = new Vector2(0, 1);
        leftRT.pivot = new Vector2(0, 0.5f);
        leftRT.anchoredPosition = Vector2.zero;
        leftRT.sizeDelta = new Vector2(borderThickness, 0);

        // Right border (vertical line at right)
        rightBorder = CreateBorderRect("RightBorder");
        var rightRT = rightBorder.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(1, 0);
        rightRT.anchorMax = new Vector2(1, 1);
        rightRT.pivot = new Vector2(1, 0.5f);
        rightRT.anchoredPosition = Vector2.zero;
        rightRT.sizeDelta = new Vector2(borderThickness, 0);
    }

    /// <summary>
    /// Create a single border rectangle GameObject.
    /// </summary>
    private GameObject CreateBorderRect(string name)
    {
        GameObject border = new GameObject(name);
        border.transform.SetParent(transform, false);

        Image img = border.AddComponent<Image>();
        img.color = highlightColor;

        return border;
    }

    /// <summary>
    /// Show the highlight rectangle at the specified world position.
    /// Calculates size dynamically to maintain proportions with world space mushrooms.
    /// </summary>
    public void Show(Vector3 worldPosition)
    {
        gameObject.SetActive(true);

        // Calculate screen pixels per world unit based on current camera zoom
        // Screen height (in pixels) / visible world height (in units) = pixels per unit
        float visibleWorldHeight = mainCamera.orthographicSize * 2f;
        float screenPixelsPerWorldUnit = Screen.height / visibleWorldHeight;

        // Convert world space highlight dimensions to screen pixels
        float worldWidth = HIGHLIGHT_WIDTH_WORLD_PIXELS / PIXELS_PER_UNIT;  // 56.7/16 = 3.54 units
        float worldHeight = HIGHLIGHT_HEIGHT_WORLD_PIXELS / PIXELS_PER_UNIT; // 77.3/16 = 4.83 units

        float screenWidth = worldWidth * screenPixelsPerWorldUnit;
        float screenHeight = worldHeight * screenPixelsPerWorldUnit;

        // Divide by Canvas scale factor to account for Canvas Scaler
        // This prevents double-scaling when window is resized
        if (canvas != null)
        {
            screenWidth /= canvas.scaleFactor;
            screenHeight /= canvas.scaleFactor;
        }

        // Update size to maintain proportions with mushrooms
        rectTransform.sizeDelta = new Vector2(screenWidth, screenHeight);

        // Convert world position to screen position for UI
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
        rectTransform.position = screenPos;
    }

    /// <summary>
    /// Hide the highlight rectangle.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
