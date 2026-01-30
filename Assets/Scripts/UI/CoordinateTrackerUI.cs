using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays the current sector coordinates in the bottom-left corner of the screen.
/// Updates in real-time as the camera moves.
/// Flashes green briefly when coordinates are updated via coordinate search.
/// </summary>
public class CoordinateTrackerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coordinateText;

    [Header("Camera Reference")]
    [SerializeField] private CameraController cameraController;

    [Header("Flash Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color flashColor = Color.green;
    [SerializeField] private float flashDuration = 1.5f;

    private bool isFlashing = false;
    private const int PIXELS_PER_UNIT = 16;

    private void Awake()
    {
        // Verify references
        if (coordinateText == null)
        {
            Debug.LogError("CoordinateTrackerUI: Coordinate Text reference is missing!");
        }
        if (cameraController == null)
        {
            Debug.LogError("CoordinateTrackerUI: Camera Controller reference is missing!");
        }

        // Set initial color
        if (coordinateText != null)
        {
            coordinateText.color = normalColor;
        }
    }

    private void Update()
    {
        UpdateCoordinateDisplay();
    }

    /// <summary>
    /// Update the coordinate display based on current camera position.
    /// </summary>
    private void UpdateCoordinateDisplay()
    {
        if (cameraController == null || coordinateText == null)
            return;

        // Get camera offset in pixel coordinates
        Vector2 cameraOffset = cameraController.CameraOffset;

        // Convert to sector coordinates (whole numbers)
        int sectorX = Mathf.FloorToInt(cameraOffset.x / PIXELS_PER_UNIT);
        int sectorY = Mathf.FloorToInt(cameraOffset.y / PIXELS_PER_UNIT);

        // Update display
        coordinateText.text = $"({sectorX}, {sectorY})";
    }

    /// <summary>
    /// Flash the coordinate display green briefly to indicate successful navigation.
    /// Called by CoordinateSearchUI when Enter is pressed.
    /// </summary>
    public void FlashGreen()
    {
        if (!isFlashing)
        {
            StartCoroutine(FlashGreenCoroutine());
        }
    }

    /// <summary>
    /// Coroutine that handles the green flash animation.
    /// </summary>
    private IEnumerator FlashGreenCoroutine()
    {
        if (coordinateText == null)
            yield break;

        isFlashing = true;

        // Flash to green
        coordinateText.color = flashColor;

        // Wait for flash duration
        yield return new WaitForSeconds(flashDuration);

        // Return to normal color
        coordinateText.color = normalColor;

        isFlashing = false;
    }
}
