using UnityEngine;

/// <summary>
/// Manages mushroom selection state.
/// Tracks the currently selected mushroom and provides events for UI updates.
/// Matches C++ variables: bMushSelected, nSelectedMushSeed1, nSelectedMushSeed2
/// </summary>
public class SelectionManager : MonoBehaviour
{
    // Scene-scoped singleton — a duplicate accidentally placed in a prefab or
    // second scene would double-emit selection events. No DontDestroyOnLoad:
    // selection state is per-scene, not persistent.
    private static SelectionManager instance;
    public static SelectionManager Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("SelectionManager: duplicate instance destroyed. Selection state should live in a single scene GameObject.");
            Destroy(this);
            return;
        }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Selection state (matches C++ variables)
    private bool isMushroomSelected = false;
    private uint selectedSeed1 = 0;
    private uint selectedSeed2 = 0;
    private MushroomData selectedMushroomData;

    // Events for UI updates (Phase 4 will use these)
    public event System.Action<MushroomData> OnMushroomSelected;
    public event System.Action OnMushroomDeselected;

    // Public accessors
    public bool IsMushroomSelected => isMushroomSelected;
    public MushroomData SelectedMushroom => selectedMushroomData;

    /// <summary>
    /// Select a mushroom at the specified tile coordinates.
    /// Matches C++ click handling logic from lines 219-227
    /// (the C++ port referred to tiles as "sectors").
    /// </summary>
    /// <param name="tileX">World tile X coordinate (seed1)</param>
    /// <param name="tileY">World tile Y coordinate (seed2)</param>
    public void SelectMushroom(uint tileX, uint tileY)
    {
        selectedSeed1 = tileX;
        selectedSeed2 = tileY;
        selectedMushroomData = MushroomData.Generate(tileX, tileY);

        if (selectedMushroomData.exists)
        {
            isMushroomSelected = true;
            OnMushroomSelected?.Invoke(selectedMushroomData);

            // Play mushroom click sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMushroomClick();
            }
        }
    }

    /// <summary>
    /// Deselect the currently selected mushroom.
    /// </summary>
    public void Deselect()
    {
        isMushroomSelected = false;
        selectedSeed1 = 0;
        selectedSeed2 = 0;
        OnMushroomDeselected?.Invoke();
    }
}
