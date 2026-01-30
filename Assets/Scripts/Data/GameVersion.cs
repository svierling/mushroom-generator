/// <summary>
/// Stores the game version constant.
/// Displayed in UI and used for save file compatibility.
/// </summary>
public static class GameVersion
{
    /// <summary>
    /// Current game version.
    /// Update this when releasing new versions.
    /// </summary>
    public const string Version = "1.1.0";

    /// <summary>
    /// Version with 'v' prefix for display purposes.
    /// </summary>
    public const string DisplayVersion = "v" + Version;
}
