using UnityEngine;

/// <summary>
/// ScriptableObject that maps MushroomType enum values to their corresponding sprite assets.
/// This allows for easy assignment in the Unity Inspector and centralized sprite management.
///
/// Draw-call batching: the referenced sprites live in Assets/Sprites/Mushrooms/
/// and are packed together by Mushrooms.spriteatlas. At runtime they share an
/// atlas texture, so Unity's sprite batcher collapses all visible mushrooms
/// into a single draw call — critical when hundreds of mushrooms are visible
/// at wide zoom. If a new mushroom sprite is added, register it in that atlas
/// asset's Packables list to preserve the batching.
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
