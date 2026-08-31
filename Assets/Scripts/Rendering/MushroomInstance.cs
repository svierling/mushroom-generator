using UnityEngine;

/// <summary>
/// Component attached to individual mushroom GameObjects.
/// Provides a simple interface for configuring position and sprite.
/// Used by object pooling system for efficient mushroom rendering.
/// </summary>
public class MushroomInstance : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("MushroomInstance requires a SpriteRenderer component!");
        }
    }

    /// <summary>
    /// Configure the mushroom at a world tile with a given sprite. The caller
    /// supplies the raw tile coordinates + height; iso projection and depth
    /// sorting happen here so the whole pool stays consistent.
    /// </summary>
    public void Configure(float worldTileX, float worldTileY, int height, Sprite sprite)
    {
        transform.position = IsoProjection.WorldToUnity(worldTileX, worldTileY, height);
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = IsoProjection.SortOrder(worldTileX, worldTileY, height);
    }
}
