using UnityEngine;

/// <summary>
/// ScriptableObject that maps MushroomType enum values to their corresponding sprite assets.
/// This allows for easy assignment in the Unity Inspector and centralized sprite management.
/// </summary>
[CreateAssetMenu(fileName = "MushroomSpriteData", menuName = "Mushroom Generator/Sprite Data")]
public class MushroomSpriteData : ScriptableObject
{
    [Header("Mushroom Sprites (46x53 pixels)")]
    [Tooltip("Bolete mushroom sprite (MushroomRed.png) - Common")]
    public Sprite boleteSprite;

    [Tooltip("Roundhead mushroom sprite (MushroomGreen.png) - Common")]
    public Sprite roundheadSprite;

    [Tooltip("Chanterelle mushroom sprite (MushroomYellow.png) - Uncommon")]
    public Sprite chanterelleSprite;

    /// <summary>
    /// Get the sprite corresponding to a mushroom type.
    /// </summary>
    /// <param name="type">The mushroom type to get the sprite for</param>
    /// <returns>The sprite asset, or null if not found</returns>
    public Sprite GetSprite(MushroomData.MushroomType type)
    {
        return type switch
        {
            MushroomData.MushroomType.Bolete => boleteSprite,
            MushroomData.MushroomType.Roundhead => roundheadSprite,
            MushroomData.MushroomType.Chanterelle => chanterelleSprite,
            _ => null
        };
    }
}
