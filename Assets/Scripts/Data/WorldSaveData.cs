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
    /// Save format version. Bump when adding/removing/reshaping fields so old
    /// saves can be migrated deterministically. Missing (older) saves
    /// deserialize as 0; new saves start at CURRENT_SCHEMA_VERSION.
    /// </summary>
    public int schemaVersion = CURRENT_SCHEMA_VERSION;

    /// <summary>Current save-format version. Increment when the shape of this class changes.</summary>
    public const int CURRENT_SCHEMA_VERSION = 1;

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
    /// Legacy field: last camera Unity position × 16, saved before iso.
    /// Still written on save for backward compat, but new code reads
    /// <see cref="lastCharacterTile"/> when present.
    /// </summary>
    public Vector2 lastCameraPosition;

    /// <summary>
    /// Character's world tile position when the world was last saved.
    /// Authoritative on load — the follow-camera derives its position from
    /// this. Defaults to (0, 0) on worlds saved before this field existed.
    /// </summary>
    public Vector2 lastCharacterTile;

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
            lastCharacterTile = Vector2.zero,
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
