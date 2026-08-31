using UnityEngine;

/// <summary>
/// One ground tile in the pooled iso grid. Positioned + sorted the same way as
/// mushrooms, but offset behind them so a mushroom at (x, y) always draws in
/// front of the tile it stands on.
/// </summary>
public class GroundTileInstance : MonoBehaviour
{
    // Draws behind the mushroom at the same tile. The offset only needs to be
    // large enough to break ties within a single tile — iso depth handles
    // cross-tile ordering via (x + y) + height already.
    private const int GROUND_LAYER_OFFSET = 10;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("GroundTileInstance requires a SpriteRenderer component!");
        }
    }

    public void Configure(int worldTileX, int worldTileY, int height, Sprite sprite)
    {
        transform.position = IsoProjection.WorldToUnity(worldTileX, worldTileY, height);
        // Sprite is generated at exact iso pixel dimensions, so local scale is
        // always 1 — this stops iso tessellation from depending on prefab scale.
        transform.localScale = Vector3.one;
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = IsoProjection.SortOrder(worldTileX, worldTileY, height) - GROUND_LAYER_OFFSET;
    }
}
