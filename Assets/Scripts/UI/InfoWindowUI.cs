using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Manages the info window that displays details about the selected mushroom.
/// Shows mushroom sprite, name, edibility, rarity, and coordinates in a horizontal card layout.
/// Subscribes to SelectionManager events to update automatically.
/// Closes when clicking outside the window or selecting another mushroom.
/// </summary>
public class InfoWindowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject windowPanel;
    [SerializeField] private Image mushroomSpriteImage;
    [SerializeField] private TextMeshProUGUI mushroomNameText;
    [SerializeField] private TextMeshProUGUI edibilityText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI coordinatesText;

    [Header("Manager References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private MushroomSpriteData spriteData;

    private void Awake()
    {
        // Verify references
        if (windowPanel == null)
        {
            Debug.LogError("InfoWindowUI: Window Panel reference is missing!");
        }
        if (mushroomSpriteImage == null)
        {
            Debug.LogError("InfoWindowUI: Mushroom Sprite Image reference is missing!");
        }
        if (mushroomNameText == null)
        {
            Debug.LogError("InfoWindowUI: Mushroom Name Text reference is missing!");
        }
        if (edibilityText == null)
        {
            Debug.LogError("InfoWindowUI: Edibility Text reference is missing!");
        }
        if (rarityText == null)
        {
            Debug.LogError("InfoWindowUI: Rarity Text reference is missing!");
        }
        if (coordinatesText == null)
        {
            Debug.LogError("InfoWindowUI: Coordinates Text reference is missing!");
        }
        if (selectionManager == null)
        {
            Debug.LogError("InfoWindowUI: Selection Manager reference is missing!");
        }
        if (spriteData == null)
        {
            Debug.LogError("InfoWindowUI: Sprite Data reference is missing!");
        }

        // Hide window initially
        Hide();
    }

    private void OnEnable()
    {
        // Subscribe to selection events
        if (selectionManager != null)
        {
            selectionManager.OnMushroomSelected += OnMushroomSelected;
            selectionManager.OnMushroomDeselected += OnMushroomDeselected;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from selection events
        if (selectionManager != null)
        {
            selectionManager.OnMushroomSelected -= OnMushroomSelected;
            selectionManager.OnMushroomDeselected -= OnMushroomDeselected;
        }
    }

    /// <summary>
    /// Called when a mushroom is selected.
    /// Updates the info window with mushroom data and shows it.
    /// </summary>
    private void OnMushroomSelected(MushroomData data)
    {
        UpdateDisplay(data);
        Show();
    }

    /// <summary>
    /// Called when a mushroom is deselected.
    /// Hides the info window.
    /// </summary>
    private void OnMushroomDeselected()
    {
        Hide();
    }

    /// <summary>
    /// Update all UI elements with mushroom data.
    /// </summary>
    private void UpdateDisplay(MushroomData data)
    {
        // Update sprite
        if (mushroomSpriteImage != null && spriteData != null)
        {
            mushroomSpriteImage.sprite = spriteData.GetSprite(data.type);
        }

        // Update text fields
        if (mushroomNameText != null)
        {
            mushroomNameText.text = data.GetName();
        }

        if (edibilityText != null)
        {
            edibilityText.text = data.GetEdibility();
        }

        if (rarityText != null)
        {
            rarityText.text = data.GetRarity();

            // Set color based on rarity (16-bit colors)
            rarityText.color = data.GetRarity() switch
            {
                "Common" => new Color32(0, 255, 0, 255),      // 16-bit Green
                "Uncommon" => new Color32(255, 255, 0, 255),  // 16-bit Yellow
                "Rare" => new Color32(255, 0, 0, 255),        // 16-bit Red
                _ => Color.white
            };
        }

        if (coordinatesText != null)
        {
            coordinatesText.text = $"({data.tileCoords.x}, {data.tileCoords.y})";
        }
    }

    /// <summary>
    /// Show the info window.
    /// </summary>
    private void Show()
    {
        if (windowPanel != null)
        {
            windowPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hide the info window.
    /// </summary>
    private void Hide()
    {
        if (windowPanel != null)
        {
            windowPanel.SetActive(false);
        }
    }
}
