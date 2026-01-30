using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// UI component to return to the main menu with confirmation dialog.
/// Triggered by Escape key, shows confirmation before returning.
/// </summary>
public class ReturnToMenuUI : MonoBehaviour
{
    [Header("Confirmation Panel")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private InputAction escapeAction;

    private void Awake()
    {
        // Setup button listeners
        if (yesButton != null)
            yesButton.onClick.AddListener(ConfirmReturnToMenu);

        if (noButton != null)
            noButton.onClick.AddListener(HideConfirmation);

        // Setup escape key using new Input System
        escapeAction = new InputAction("Escape", InputActionType.Button, "<Keyboard>/escape");
        escapeAction.performed += OnEscapePressed;

        // Hide panel initially
        HideConfirmation();
    }

    private void OnEnable()
    {
        escapeAction?.Enable();
    }

    private void OnDisable()
    {
        escapeAction?.Disable();
    }

    private void OnDestroy()
    {
        if (escapeAction != null)
        {
            escapeAction.performed -= OnEscapePressed;
            escapeAction.Dispose();
        }
    }

    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        if (isPaused)
        {
            HideConfirmation();
        }
        else
        {
            ShowConfirmation();
        }
    }

    /// <summary>
    /// Show the confirmation dialog.
    /// </summary>
    public void ShowConfirmation()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
            isPaused = true;

            // Select "No" button by default for safety
            if (noButton != null)
            {
                noButton.Select();
            }
        }
    }

    /// <summary>
    /// Hide the confirmation dialog.
    /// </summary>
    public void HideConfirmation()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
            isPaused = false;
        }
    }

    /// <summary>
    /// Confirm and return to the main menu.
    /// </summary>
    public void ConfirmReturnToMenu()
    {
        if (WorldManager.Instance != null && WorldManager.Instance.HasActiveWorld)
        {
            // Save camera position BEFORE unloading the world
            // (OnDisable runs after UnloadCurrentWorld clears CurrentWorld)
            var cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController != null)
            {
                WorldManager.Instance.SaveCameraPosition(cameraController.CameraOffset);
            }

            WorldManager.Instance.UnloadCurrentWorld();
        }

        isPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
