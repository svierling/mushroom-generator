using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Main menu UI controller.
/// Handles world creation, loading, and theme music.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    [SerializeField] private Button newGenerationButton;
    [SerializeField] private Button loadGenerationButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    [Header("New World Panel")]
    [SerializeField] private GameObject newWorldPanel;
    [SerializeField] private TMP_InputField worldNameInput;
    [SerializeField] private Button createWorldButton;
    [SerializeField] private Button cancelNewWorldButton;
    [SerializeField] private TextMeshProUGUI newWorldErrorText;

    [Header("Plot Size Selector (optional; wire in Editor for UI)")]
    [Tooltip("Toggle for the Small plot (256×256 tiles). Optional — if none of the three toggles are wired, Medium is used as the default.")]
    [SerializeField] private Toggle plotSizeSmallToggle;
    [Tooltip("Toggle for the Medium plot (512×512 tiles). The default plot size.")]
    [SerializeField] private Toggle plotSizeMediumToggle;
    [Tooltip("Toggle for the Large plot (1024×1024 tiles).")]
    [SerializeField] private Toggle plotSizeLargeToggle;

    [Header("Load World Panel")]
    [SerializeField] private GameObject loadWorldPanel;
    [SerializeField] private Transform worldListContent;
    [SerializeField] private GameObject worldListItemPrefab;
    [SerializeField] private Button cancelLoadButton;

    [Header("Delete Confirmation Panel")]
    [SerializeField] private GameObject deleteConfirmPanel;
    [SerializeField] private TextMeshProUGUI deleteConfirmText;
    [SerializeField] private Button deleteYesButton;
    [SerializeField] private Button deleteNoButton;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip themeSong;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "MainScene";

    private string worldPendingDeletion;

    private void Start()
    {
        // Setup button listeners
        SetupButtons();

        // Hide panels initially
        HideAllPanels();

        // Update continue button state
        UpdateContinueButton();

        // Start theme music
        PlayThemeMusic();
    }

    private void SetupButtons()
    {
        if (newGenerationButton != null)
            newGenerationButton.onClick.AddListener(OnNewGenerationClicked);

        if (loadGenerationButton != null)
            loadGenerationButton.onClick.AddListener(OnLoadGenerationClicked);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (createWorldButton != null)
            createWorldButton.onClick.AddListener(OnCreateWorldClicked);

        if (cancelNewWorldButton != null)
            cancelNewWorldButton.onClick.AddListener(HideAllPanels);

        if (cancelLoadButton != null)
            cancelLoadButton.onClick.AddListener(HideAllPanels);

        if (deleteYesButton != null)
            deleteYesButton.onClick.AddListener(ConfirmDeleteWorld);

        if (deleteNoButton != null)
            deleteNoButton.onClick.AddListener(CancelDeleteWorld);
    }

    private void HideAllPanels()
    {
        if (newWorldPanel != null)
            newWorldPanel.SetActive(false);
        if (loadWorldPanel != null)
            loadWorldPanel.SetActive(false);
        if (newWorldErrorText != null)
            newWorldErrorText.gameObject.SetActive(false);
        if (deleteConfirmPanel != null)
            deleteConfirmPanel.SetActive(false);
        worldPendingDeletion = null;
    }

    /// <summary>
    /// Update Continue button interactability based on whether a last world exists.
    /// </summary>
    private void UpdateContinueButton()
    {
        if (continueButton != null && WorldManager.Instance != null)
        {
            string lastWorld = WorldManager.Instance.GetLastPlayedWorldName();
            continueButton.interactable = !string.IsNullOrEmpty(lastWorld);
        }
    }

    /// <summary>
    /// Start playing the theme music on loop.
    /// </summary>
    private void PlayThemeMusic()
    {
        if (musicSource != null && themeSong != null)
        {
            musicSource.clip = themeSong;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Stop the theme music.
    /// </summary>
    private void StopThemeMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    #region Button Handlers

    private void OnNewGenerationClicked()
    {
        HideAllPanels();
        if (newWorldPanel != null)
        {
            newWorldPanel.SetActive(true);
            if (worldNameInput != null)
            {
                worldNameInput.text = "";
                worldNameInput.Select();
            }
        }
    }

    private void OnLoadGenerationClicked()
    {
        HideAllPanels();
        if (loadWorldPanel != null)
        {
            loadWorldPanel.SetActive(true);
            PopulateWorldList();
        }
    }

    private void OnContinueClicked()
    {
        if (WorldManager.Instance == null)
            return;

        string lastWorld = WorldManager.Instance.GetLastPlayedWorldName();
        if (!string.IsNullOrEmpty(lastWorld))
        {
            LoadWorld(lastWorld);
        }
    }

    private void OnQuitClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void OnCreateWorldClicked()
    {
        if (worldNameInput == null || WorldManager.Instance == null)
            return;

        string worldName = worldNameInput.text.Trim();

        // Validate world name
        if (string.IsNullOrEmpty(worldName))
        {
            ShowNewWorldError("Please enter a world name.");
            return;
        }

        if (worldName.Length > 50)
        {
            ShowNewWorldError("World name must be 50 characters or less.");
            return;
        }

        if (WorldManager.Instance.WorldExists(worldName))
        {
            ShowNewWorldError("A world with this name already exists.");
            return;
        }

        // Create and load the new world
        var newWorld = WorldManager.Instance.CreateNewWorld(worldName, GetSelectedPlotSize());
        WorldManager.Instance.SetCurrentWorld(newWorld);

        // Load gameplay scene
        StopThemeMusic();
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>
    /// Read the plot-size toggles (if wired). Falls back to Medium when no
    /// toggle is wired or none is checked — this keeps the New World panel
    /// working on scenes that haven't been updated to include the selector.
    /// </summary>
    private PlotSize GetSelectedPlotSize()
    {
        if (plotSizeSmallToggle != null && plotSizeSmallToggle.isOn) return PlotSize.Small;
        if (plotSizeLargeToggle != null && plotSizeLargeToggle.isOn) return PlotSize.Large;
        return PlotSize.Medium;
    }

    private void ShowNewWorldError(string message)
    {
        if (newWorldErrorText != null)
        {
            newWorldErrorText.text = message;
            newWorldErrorText.gameObject.SetActive(true);
        }
    }

    #endregion

    #region World List

    private void EnsureContentLayout()
    {
        if (worldListContent == null)
            return;

        // First, ensure Viewport is properly configured (parent of Content)
        var viewport = worldListContent.parent;
        if (viewport != null)
        {
            var viewportRect = viewport.GetComponent<RectTransform>();
            if (viewportRect != null)
            {
                // Viewport should stretch to fill ScrollView
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.offsetMin = Vector2.zero;
                viewportRect.offsetMax = Vector2.zero;
            }
        }

        // Configure Content RectTransform
        var contentRect = worldListContent.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            // Anchor to top, stretch horizontally
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0); // Width from anchors, height from ContentSizeFitter
        }

        // Ensure Content has Vertical Layout Group
        var layoutGroup = worldListContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = worldListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 5;
        layoutGroup.padding = new RectOffset(5, 5, 5, 5);

        // Ensure Content has Content Size Fitter
        var sizeFitter = worldListContent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = worldListContent.gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void PopulateWorldList()
    {
        if (worldListContent == null || worldListItemPrefab == null || WorldManager.Instance == null)
            return;

        // Ensure Content has proper layout components
        EnsureContentLayout();

        // Clear existing items
        foreach (Transform child in worldListContent)
        {
            Destroy(child.gameObject);
        }

        // Get all worlds
        List<WorldSaveData> worlds = WorldManager.Instance.GetAllWorlds();

        if (worlds.Count == 0)
        {
            // Show "No worlds found" message
            var emptyItem = Instantiate(worldListItemPrefab, worldListContent);
            var itemText = emptyItem.GetComponentInChildren<TextMeshProUGUI>();
            if (itemText != null)
            {
                itemText.text = "No saved worlds found.";
            }
            var itemButton = emptyItem.GetComponent<Button>();
            if (itemButton != null)
            {
                itemButton.interactable = false;
            }
            return;
        }

        // Create list items
        foreach (var world in worlds)
        {
            CreateWorldListItem(world);
        }
    }

    private void CreateWorldListItem(WorldSaveData world)
    {
        if (worldListContent == null || worldListItemPrefab == null)
            return;

        var item = Instantiate(worldListItemPrefab, worldListContent);

        // Ensure item has an Image component for visibility
        var itemImage = item.GetComponent<Image>();
        if (itemImage == null)
        {
            itemImage = item.AddComponent<Image>();
        }
        itemImage.color = new Color32(100, 100, 140, 255); // Visible button background

        // Ensure item has a Button component
        var itemButton = item.GetComponent<Button>();
        if (itemButton == null)
        {
            itemButton = item.AddComponent<Button>();
        }
        itemButton.targetGraphic = itemImage;

        // Set button normal color and apply consistent styling
        var buttonColors = itemButton.colors;
        buttonColors.normalColor = new Color32(100, 100, 140, 255);
        itemButton.colors = buttonColors;
        ButtonStyler.ApplyDefaultStyle(itemButton);

        // Ensure item has a Layout Element for proper sizing in the list
        var layoutElement = item.GetComponent<UnityEngine.UI.LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = item.AddComponent<UnityEngine.UI.LayoutElement>();
        }
        layoutElement.minHeight = 60;
        layoutElement.preferredHeight = 60;

        // Find or create the text component
        var itemText = item.GetComponentInChildren<TextMeshProUGUI>();
        if (itemText == null)
        {
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(item.transform, false);
            itemText = textGO.AddComponent<TextMeshProUGUI>();
        }

        string lastPlayed = world.GetLastPlayed().ToLocalTime().ToString("g");
        itemText.text = $"{world.worldName}\n<size=80%><color=#888888>Last played: {lastPlayed}</color></size>";

        // Configure text
        itemText.enableAutoSizing = false;
        itemText.fontSize = 18;
        itemText.alignment = TextAlignmentOptions.Left;
        itemText.overflowMode = TextOverflowModes.Ellipsis;
        itemText.enableWordWrapping = true;
        itemText.color = Color.white;

        // Ensure RectTransform stretches to fill button (leaving room for delete button)
        var textRect = itemText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5); // Left, Bottom padding
        textRect.offsetMax = new Vector2(-50, -5); // Right padding for delete button, Top padding

        // Setup load button click handler
        string worldName = world.worldName; // Capture for closure
        itemButton.onClick.AddListener(() => LoadWorld(worldName));

        // Create delete button
        CreateDeleteButton(item.transform, worldName);
    }

    private void LoadWorld(string worldName)
    {
        if (WorldManager.Instance == null)
            return;

        if (WorldManager.Instance.LoadWorld(worldName))
        {
            StopThemeMusic();
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            Debug.LogError($"MainMenuUI: Failed to load world '{worldName}'");
        }
    }

    private void CreateDeleteButton(Transform parent, string worldName)
    {
        // Create delete button GameObject
        var deleteButtonGO = new GameObject("DeleteButton");
        deleteButtonGO.transform.SetParent(parent, false);

        // Add Image component for button visuals
        var image = deleteButtonGO.AddComponent<Image>();
        image.color = new Color32(180, 60, 60, 255); // Red-ish color

        // Add Button component with consistent styling
        var button = deleteButtonGO.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color32(180, 60, 60, 255);
        button.colors = colors;
        button.targetGraphic = image;
        ButtonStyler.ApplyDefaultStyle(button);

        // Position in top-right corner
        var rectTransform = deleteButtonGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 0.5f);
        rectTransform.anchorMax = new Vector2(1, 0.5f);
        rectTransform.pivot = new Vector2(1, 0.5f);
        rectTransform.anchoredPosition = new Vector2(-8, 0);
        rectTransform.sizeDelta = new Vector2(36, 36);

        // Add "X" text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(deleteButtonGO.transform, false);

        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "X";
        text.fontSize = 20;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Setup click handler
        string capturedName = worldName; // Capture for closure
        button.onClick.AddListener(() => ShowDeleteConfirmation(capturedName));
    }

    private void ShowDeleteConfirmation(string worldName)
    {
        worldPendingDeletion = worldName;

        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(true);

            if (deleteConfirmText != null)
            {
                deleteConfirmText.text = $"Delete world \"{worldName}\"?\nThis cannot be undone.";
            }

            // Select "No" by default for safety
            if (deleteNoButton != null)
            {
                deleteNoButton.Select();
            }
        }
    }

    private void ConfirmDeleteWorld()
    {
        if (!string.IsNullOrEmpty(worldPendingDeletion) && WorldManager.Instance != null)
        {
            WorldManager.Instance.DeleteWorld(worldPendingDeletion);
            worldPendingDeletion = null;

            // Hide confirmation and refresh list
            if (deleteConfirmPanel != null)
                deleteConfirmPanel.SetActive(false);

            PopulateWorldList();
            UpdateContinueButton();
        }
    }

    private void CancelDeleteWorld()
    {
        worldPendingDeletion = null;
        if (deleteConfirmPanel != null)
            deleteConfirmPanel.SetActive(false);
    }

    #endregion
}
