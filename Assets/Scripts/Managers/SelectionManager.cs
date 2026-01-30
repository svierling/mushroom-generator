using UnityEngine;

/// <summary>
/// Manages mushroom selection state.
/// Tracks the currently selected mushroom and provides events for UI updates.
/// Matches C++ variables: bMushSelected, nSelectedMushSeed1, nSelectedMushSeed2
/// </summary>
public class SelectionManager : MonoBehaviour
{
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
    /// Select a mushroom at the specified sector coordinates.
    /// Matches C++ click handling logic from lines 219-227.
    /// </summary>
    /// <param name="sectorX">World sector X coordinate (seed1)</param>
    /// <param name="sectorY">World sector Y coordinate (seed2)</param>
    public void SelectMushroom(uint sectorX, uint sectorY)
    {
        selectedSeed1 = sectorX;
        selectedSeed2 = sectorY;
        selectedMushroomData = MushroomData.Generate(sectorX, sectorY);

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
