using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the game version in the top-left corner of the screen.
/// Persists across all scenes using DontDestroyOnLoad.
/// Creates its own canvas to ensure visibility in all scenes.
/// </summary>
public class VersionDisplay : MonoBehaviour
{
    private static VersionDisplay instance;

    [Header("Font Settings")]
    [SerializeField] private TMP_FontAsset customFont;
    [SerializeField] private int fontSize = 18;
    [SerializeField] private Color textColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private TextMeshProUGUI versionText;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Create the UI if it doesn't exist
        CreateVersionUI();
    }

    private void CreateVersionUI()
    {
        // Check if we already have a canvas child
        var existingCanvas = GetComponentInChildren<Canvas>();
        if (existingCanvas != null)
        {
            versionText = GetComponentInChildren<TextMeshProUGUI>();
            if (versionText != null && customFont != null)
            {
                versionText.font = customFont;
            }
            UpdateVersionText();
            return;
        }

        // Create Canvas
        var canvasGO = new GameObject("VersionCanvas");
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Ensure it's on top

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Create Text
        var textGO = new GameObject("VersionText");
        textGO.transform.SetParent(canvasGO.transform);

        versionText = textGO.AddComponent<TextMeshProUGUI>();

        // Apply custom font if assigned
        if (customFont != null)
        {
            versionText.font = customFont;
        }

        versionText.fontSize = fontSize;
        versionText.color = textColor;
        versionText.alignment = TextAlignmentOptions.TopLeft;

        // Position in top-left
        var rectTransform = versionText.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(15, -15);
        rectTransform.sizeDelta = new Vector2(200, 40);

        UpdateVersionText();
    }

    /// <summary>
    /// Update the displayed version text.
    /// </summary>
    public void UpdateVersionText()
    {
        if (versionText != null)
        {
            versionText.text = GameVersion.DisplayVersion;
        }
    }
}
