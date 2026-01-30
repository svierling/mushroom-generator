using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically applies consistent hover/press colors to all buttons in the scene.
/// Attach to a GameObject in each scene (e.g., Canvas or a UI manager).
/// </summary>
public class ButtonStyler : MonoBehaviour
{
    [Header("Color Adjustments")]
    [SerializeField, Range(0f, 0.3f)] private float hoverDarkenAmount = 0.08f;
    [SerializeField, Range(0f, 0.5f)] private float pressDarkenAmount = 0.15f;
    [SerializeField, Range(0f, 0.3f)] private float selectedDarkenAmount = 0.05f;

    [Header("Options")]
    [SerializeField] private bool styleOnStart = true;
    [SerializeField] private bool includeInactiveButtons = true;

    private void Start()
    {
        if (styleOnStart)
        {
            StyleAllButtons();
        }
    }

    /// <summary>
    /// Apply consistent styling to all buttons in the scene.
    /// </summary>
    public void StyleAllButtons()
    {
        Button[] buttons = includeInactiveButtons
            ? Resources.FindObjectsOfTypeAll<Button>()
            : FindObjectsByType<Button>(FindObjectsSortMode.None);

        foreach (var button in buttons)
        {
            // Skip buttons that are part of prefabs in the project (not in scene)
            if (!button.gameObject.scene.isLoaded)
                continue;

            StyleButton(button);
        }
    }

    /// <summary>
    /// Apply styling to a single button based on its current normal color.
    /// </summary>
    public void StyleButton(Button button)
    {
        if (button == null)
            return;

        var colors = button.colors;
        Color normalColor = colors.normalColor;

        // Calculate darker shades for hover, press, and selected states
        colors.highlightedColor = DarkenColor(normalColor, hoverDarkenAmount);
        colors.pressedColor = DarkenColor(normalColor, pressDarkenAmount);
        colors.selectedColor = DarkenColor(normalColor, selectedDarkenAmount);

        button.colors = colors;
    }

    /// <summary>
    /// Darken a color by the specified amount (0-1).
    /// </summary>
    private Color DarkenColor(Color color, float amount)
    {
        return new Color(
            Mathf.Max(0, color.r - amount),
            Mathf.Max(0, color.g - amount),
            Mathf.Max(0, color.b - amount),
            color.a
        );
    }

    /// <summary>
    /// Static utility to style a button with default values.
    /// Can be called from other scripts.
    /// </summary>
    public static void ApplyDefaultStyle(Button button)
    {
        if (button == null)
            return;

        var colors = button.colors;
        Color normalColor = colors.normalColor;

        colors.highlightedColor = new Color(
            Mathf.Max(0, normalColor.r - 0.08f),
            Mathf.Max(0, normalColor.g - 0.08f),
            Mathf.Max(0, normalColor.b - 0.08f),
            normalColor.a
        );
        colors.pressedColor = new Color(
            Mathf.Max(0, normalColor.r - 0.15f),
            Mathf.Max(0, normalColor.g - 0.15f),
            Mathf.Max(0, normalColor.b - 0.15f),
            normalColor.a
        );
        colors.selectedColor = new Color(
            Mathf.Max(0, normalColor.r - 0.05f),
            Mathf.Max(0, normalColor.g - 0.05f),
            Mathf.Max(0, normalColor.b - 0.05f),
            normalColor.a
        );

        button.colors = colors;
    }
}
