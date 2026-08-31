using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structure for saving and loading world state.
/// Serialized to JSON for persistence.
/// </summary>
[Serializable]
public class WorldSaveData
{
    /// <summary>
    /// User-provided name for this world.
    /// </summary>
    public string worldName;

    /// <summary>
    /// Seed used for procedural generation.
    /// Different seeds produce different worlds.
    /// </summary>
    public uint worldSeed;

    /// <summary>
    /// Last camera position when the world was saved.
    /// Used to restore view on load.
    /// </summary>
    public Vector2 lastCameraPosition;

    /// <summary>
    /// Timestamp of when this world was last played.
    /// Used for sorting and "Continue" feature.
    /// </summary>
    public string lastPlayedUtc;

    /// <summary>
    /// Discrete zoom level index — indexes into CameraController.zoomLevels.
    /// Worlds saved before zoom existed deserialize as 0 (0.5x); users can
    /// bump back to 1x with the number keys and the save catches up.
    /// </summary>
    public int lastZoomIndex;

    /// <summary>
    /// Get the last played time as DateTime.
    /// </summary>
    public DateTime GetLastPlayed()
    {
        if (DateTime.TryParse(lastPlayedUtc, out DateTime result))
            return result;
        return DateTime.MinValue;
    }

    /// <summary>
    /// Set the last played time.
    /// </summary>
    public void SetLastPlayed(DateTime time)
    {
        lastPlayedUtc = time.ToString("o"); // ISO 8601 format
    }

    /// <summary>
    /// Create a new world with a random seed.
    /// </summary>
    public static WorldSaveData CreateNew(string name)
    {
        return new WorldSaveData
        {
            worldName = name,
            worldSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue),
            lastCameraPosition = Vector2.zero,
            lastPlayedUtc = DateTime.UtcNow.ToString("o"),
            lastZoomIndex = 1
        };
    }
}

/// <summary>
/// Container for all saved worlds.
/// </summary>
[Serializable]
public class WorldSaveDataList
{
    public List<WorldSaveData> worlds = new List<WorldSaveData>();

    /// <summary>
    /// Name of the last played world (for Continue button).
    /// </summary>
    public string lastPlayedWorldName;
}
