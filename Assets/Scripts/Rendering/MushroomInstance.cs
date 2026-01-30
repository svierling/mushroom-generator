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
    /// Configure the mushroom's position and sprite.
    /// Called by MushroomGenerator when spawning from object pool.
    /// </summary>
    /// <param name="position">World position to place the mushroom</param>
    /// <param name="sprite">Sprite to display</param>
    public void Configure(Vector3 position, Sprite sprite)
    {
        transform.position = position;
        spriteRenderer.sprite = sprite;

        // Y-sorting: Lower Y positions render in front (appear closer)
        // Multiply by 100 for sufficient granularity, negate so lower Y = higher priority
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-position.y * 100f);
    }
}
