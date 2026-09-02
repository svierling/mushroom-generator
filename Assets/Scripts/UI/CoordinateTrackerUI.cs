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
    [Tooltip("Color used by FlashGreen — successful teleport.")]
    [SerializeField] private Color flashColor = Color.green;
    [Tooltip("Color used by FlashRed — rejected navigation (e.g. coordinates outside the plot).")]
    [SerializeField] private Color flashRejectColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float flashDuration = 1.5f;

    private bool isFlashing = false;

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
    /// Update the coordinate display based on the character's current tile.
    /// In iso the camera position no longer corresponds to a meaningful tile —
    /// the character is the anchor now.
    /// </summary>
    private void UpdateCoordinateDisplay()
    {
        if (cameraController == null || cameraController.Target == null || coordinateText == null)
            return;

        int tileX = Mathf.FloorToInt(cameraController.Target.WorldTileX);
        int tileY = Mathf.FloorToInt(cameraController.Target.WorldTileY);

        coordinateText.text = $"({tileX}, {tileY})";
    }

    /// <summary>
    /// Flash the coordinate display green briefly to indicate successful navigation.
    /// Called by CoordinateSearchUI when Enter is pressed and coordinates are valid.
    /// </summary>
    public void FlashGreen() => Flash(flashColor);

    /// <summary>
    /// Flash the coordinate display red briefly to indicate rejected navigation
    /// (e.g. the entered coordinates fell outside the plot boundary).
    /// </summary>
    public void FlashRed() => Flash(flashRejectColor);

    private void Flash(Color color)
    {
        if (!isFlashing)
        {
            StartCoroutine(FlashCoroutine(color));
        }
    }

    private IEnumerator FlashCoroutine(Color color)
    {
        if (coordinateText == null)
            yield break;

        isFlashing = true;
        coordinateText.color = color;
        yield return new WaitForSeconds(flashDuration);
        coordinateText.color = normalColor;
        isFlashing = false;
    }
}
