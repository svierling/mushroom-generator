using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the coordinate search UI that allows users to jump to specific world coordinates.
/// Features a magnifying glass button that toggles X/Y input fields.
/// Pressing Enter navigates the camera to the entered coordinates.
/// </summary>
public class CoordinateSearchUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button searchButton;
    [SerializeField] private GameObject inputFieldsContainer;
    [SerializeField] private TMP_InputField xInputField;
    [SerializeField] private TMP_InputField yInputField;

    [Header("Camera Reference")]
    [SerializeField] private CameraController cameraController;

    [Header("Coordinate Tracker")]
    [SerializeField] private CoordinateTrackerUI coordinateTracker;

    private InputActions inputActions;
    private UnityEngine.InputSystem.InputAction submitAction;
    private UnityEngine.InputSystem.InputAction navigateAction;
    private bool isInputVisible = false;

    private void Awake()
    {
        // Verify references
        if (searchButton == null)
        {
            Debug.LogError("CoordinateSearchUI: Search Button reference is missing!");
        }
        if (inputFieldsContainer == null)
        {
            Debug.LogError("CoordinateSearchUI: Input Fields Container reference is missing!");
        }
        if (xInputField == null)
        {
            Debug.LogError("CoordinateSearchUI: X Input Field reference is missing!");
        }
        if (yInputField == null)
        {
            Debug.LogError("CoordinateSearchUI: Y Input Field reference is missing!");
        }
        if (cameraController == null)
        {
            Debug.LogError("CoordinateSearchUI: Camera Controller reference is missing!");
        }
        if (coordinateTracker == null)
        {
            Debug.LogWarning("CoordinateSearchUI: Coordinate Tracker reference is missing - flash effect will not work.");
        }

        // Setup button click handler
        if (searchButton != null)
        {
            searchButton.onClick.AddListener(ToggleInputFields);
        }

        // Setup input field defaults
        if (xInputField != null)
        {
            xInputField.text = "0";
            xInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            xInputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
        }
        if (yInputField != null)
        {
            yInputField.text = "0";
            yInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            yInputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
        }

        // Setup input actions for Enter and Tab keys
        inputActions = new InputActions();
        submitAction = inputActions.UI.Submit;
        submitAction.performed += OnSubmit;
        navigateAction = inputActions.UI.Navigate;
        navigateAction.performed += OnNavigate;

        // Hide input fields initially
        HideInputFields();
    }

    private void OnEnable()
    {
        submitAction.Enable();
        navigateAction.Enable();
    }

    private void OnDisable()
    {
        submitAction.Disable();
        navigateAction.Disable();
    }

    /// <summary>
    /// Toggle visibility of X/Y input fields when magnifying glass is clicked.
    /// </summary>
    private void ToggleInputFields()
    {
        // Play UI click sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }

        if (isInputVisible)
        {
            HideInputFields();
        }
        else
        {
            ShowInputFields();
        }
    }

    /// <summary>
    /// Show the input fields and focus on X field.
    /// </summary>
    private void ShowInputFields()
    {
        if (inputFieldsContainer != null)
        {
            inputFieldsContainer.SetActive(true);
            isInputVisible = true;

            // Focus on X input field for immediate typing
            if (xInputField != null)
            {
                xInputField.Select();
            }
        }
    }

    /// <summary>
    /// Hide the input fields.
    /// </summary>
    private void HideInputFields()
    {
        if (inputFieldsContainer != null)
        {
            inputFieldsContainer.SetActive(false);
            isInputVisible = false;
        }
    }

    /// <summary>
    /// Handle Enter key press - navigate to entered coordinates.
    /// </summary>
    private void OnSubmit(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // Only process if input fields are visible and one is focused
        if (!isInputVisible)
            return;

        // Check if either input field is focused
        bool isXFocused = (xInputField != null && xInputField.isFocused);
        bool isYFocused = (yInputField != null && yInputField.isFocused);

        if (!isXFocused && !isYFocused)
            return;

        // Parse coordinates (default to 0 if empty or invalid)
        int targetX = 0;
        int targetY = 0;

        if (xInputField != null)
        {
            if (string.IsNullOrEmpty(xInputField.text))
                xInputField.text = "0";
            int.TryParse(xInputField.text, out targetX);
        }

        if (yInputField != null)
        {
            if (string.IsNullOrEmpty(yInputField.text))
                yInputField.text = "0";
            int.TryParse(yInputField.text, out targetY);
        }

        // Navigate to coordinates
        NavigateToCoordinates(targetX, targetY);

        // Hide input fields after navigation
        HideInputFields();
    }

    /// <summary>
    /// Teleport the character to the given world tile. Rejects (with a red
    /// tracker flash) coordinates that fall outside the plot boundary — the
    /// PlayerController.TeleportToTile clamp is a safety net, but silently
    /// clamping would be a confusing UX (typing "9999" and landing at 255).
    /// The follow-camera snaps on top via the deadzone-follow next frame.
    /// </summary>
    private void NavigateToCoordinates(int tileX, int tileY)
    {
        if (cameraController == null || cameraController.Target == null)
            return;

        if (!WorldBounds.Contains(tileX, tileY))
        {
            if (coordinateTracker != null) coordinateTracker.FlashRed();
            return;
        }

        cameraController.Target.TeleportToTile(tileX, tileY);
        if (coordinateTracker != null) coordinateTracker.FlashGreen();
    }

    /// <summary>
    /// Handle Tab key press - toggle focus between X and Y fields.
    /// </summary>
    private void OnNavigate(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // Only process if input fields are visible
        if (!isInputVisible)
            return;

        // If X field is focused, move to Y field
        if (xInputField != null && xInputField.isFocused)
        {
            if (yInputField != null)
            {
                yInputField.Select();
            }
        }
        // If Y field is focused, move back to X field
        else if (yInputField != null && yInputField.isFocused)
        {
            if (xInputField != null)
            {
                xInputField.Select();
            }
        }
    }
}
