using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton manager for world state and persistence.
/// Persists across scene loads to maintain world data.
/// </summary>
public class WorldManager : MonoBehaviour
{
    private static WorldManager instance;
    public static WorldManager Instance => instance;

    /// <summary>
    /// Currently loaded world data. Null if no world is loaded.
    /// </summary>
    public WorldSaveData CurrentWorld { get; private set; }

    /// <summary>
    /// Whether a world is currently loaded and active.
    /// </summary>
    public bool HasActiveWorld => CurrentWorld != null;

    /// <summary>
    /// Current world seed. Returns 0 if no world is loaded (for backwards compatibility).
    /// </summary>
    public uint WorldSeed => CurrentWorld?.worldSeed ?? 0;

    private WorldSaveDataList saveDataList;
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "worlds.json");

    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSaveData();
    }

    /// <summary>
    /// Load the save data file from disk. Migrates each world's payload up to
    /// <see cref="WorldSaveData.CURRENT_SCHEMA_VERSION"/> before use so
    /// downstream code never sees legacy field defaults.
    /// </summary>
    private void LoadSaveData()
    {
        if (File.Exists(SaveFilePath))
        {
            try
            {
                string json = File.ReadAllText(SaveFilePath);
                saveDataList = JsonUtility.FromJson<WorldSaveDataList>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"WorldManager: Failed to load save data: {e.Message}");
                saveDataList = new WorldSaveDataList();
            }
        }
        else
        {
            saveDataList = new WorldSaveDataList();
        }

        // Migrate all worlds up to the current schema (fills in defaults for
        // fields introduced by later versions, e.g. plotSideTiles).
        if (saveDataList != null && saveDataList.worlds != null)
        {
            for (int i = 0; i < saveDataList.worlds.Count; i++)
            {
                saveDataList.worlds[i]?.Migrate();
            }
        }
    }

    /// <summary>
    /// Save the save data file to disk.
    /// </summary>
    private void SaveDataToDisk()
    {
        try
        {
            string json = JsonUtility.ToJson(saveDataList, true);
            File.WriteAllText(SaveFilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"WorldManager: Failed to save data: {e.Message}");
        }
    }

    /// <summary>
    /// Create a new world with the given name and plot size (default Small).
    /// </summary>
    public WorldSaveData CreateNewWorld(string worldName, PlotSize plotSize = PlotSize.Small)
    {
        var newWorld = WorldSaveData.CreateNew(worldName, plotSize);
        saveDataList.worlds.Add(newWorld);
        SaveDataToDisk();
        return newWorld;
    }

    /// <summary>
    /// Load an existing world by name.
    /// </summary>
    public bool LoadWorld(string worldName)
    {
        var world = saveDataList.worlds.FirstOrDefault(w => w.worldName == worldName);
        if (world != null)
        {
            CurrentWorld = world;
            CurrentWorld.SetLastPlayed(System.DateTime.UtcNow);
            saveDataList.lastPlayedWorldName = worldName;
            SaveDataToDisk();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Set the current world directly (used after creating a new world).
    /// </summary>
    public void SetCurrentWorld(WorldSaveData world)
    {
        CurrentWorld = world;
        CurrentWorld.SetLastPlayed(System.DateTime.UtcNow);
        saveDataList.lastPlayedWorldName = world.worldName;
        SaveDataToDisk();
    }

    /// <summary>
    /// Save the current world's camera position.
    /// </summary>
    public void SaveCameraPosition(Vector2 position)
    {
        if (CurrentWorld != null)
        {
            CurrentWorld.lastCameraPosition = position;
            CurrentWorld.SetLastPlayed(System.DateTime.UtcNow);
            SaveDataToDisk();
        }
    }

    /// <summary>
    /// Save the current world's character tile position — authoritative for
    /// where the player is on load. See <see cref="WorldSaveData.lastCharacterTile"/>.
    /// </summary>
    public void SaveCharacterTile(Vector2 tile)
    {
        if (CurrentWorld != null)
        {
            CurrentWorld.lastCharacterTile = tile;
            CurrentWorld.SetLastPlayed(System.DateTime.UtcNow);
            SaveDataToDisk();
        }
    }

    /// <summary>
    /// Save the current world's zoom level index.
    /// </summary>
    public void SaveZoomIndex(int index)
    {
        if (CurrentWorld != null)
        {
            CurrentWorld.lastZoomIndex = index;
            CurrentWorld.SetLastPlayed(System.DateTime.UtcNow);
            SaveDataToDisk();
        }
    }

    /// <summary>
    /// Get all saved worlds, sorted by last played (most recent first).
    /// </summary>
    public List<WorldSaveData> GetAllWorlds()
    {
        return saveDataList.worlds
            .OrderByDescending(w => w.GetLastPlayed())
            .ToList();
    }

    /// <summary>
    /// Get the name of the last played world (for Continue button).
    /// Returns null if no world has been played.
    /// </summary>
    public string GetLastPlayedWorldName()
    {
        return saveDataList.lastPlayedWorldName;
    }

    /// <summary>
    /// Check if a world with the given name exists.
    /// </summary>
    public bool WorldExists(string worldName)
    {
        return saveDataList.worlds.Any(w => w.worldName == worldName);
    }

    /// <summary>
    /// Delete a world by name.
    /// </summary>
    public bool DeleteWorld(string worldName)
    {
        var world = saveDataList.worlds.FirstOrDefault(w => w.worldName == worldName);
        if (world != null)
        {
            saveDataList.worlds.Remove(world);
            if (saveDataList.lastPlayedWorldName == worldName)
            {
                saveDataList.lastPlayedWorldName = null;
            }
            if (CurrentWorld?.worldName == worldName)
            {
                CurrentWorld = null;
            }
            SaveDataToDisk();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Unload the current world (return to main menu).
    /// </summary>
    public void UnloadCurrentWorld()
    {
        if (CurrentWorld != null)
        {
            SaveDataToDisk();
            CurrentWorld = null;
        }
    }

    private void OnApplicationQuit()
    {
        // Auto-save on quit
        if (CurrentWorld != null)
        {
            SaveDataToDisk();
        }
    }
}
