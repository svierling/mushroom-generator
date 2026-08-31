using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Main-character sprite. Reads Player.Move/Player.Sprint, updates a float
/// tile position, projects to Unity space via <see cref="IsoProjection"/>, and
/// keeps its sort order in sync with mushrooms and ground tiles.
///
/// Screen-relative WASD: W moves the character up on screen, D moves right, etc.
/// The input vector is un-projected through the iso transform so the character
/// tracks the intuitive on-screen direction regardless of the world grid.
///
/// If no sprite is assigned, a colored capsule placeholder is generated at
/// Start(). Swap it out later for a proper sprite sheet.
///
/// Named <c>PlayerController</c> (not <c>CharacterController</c>) to avoid
/// clashing with <see cref="UnityEngine.CharacterController"/>.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Screen-space movement speed in Unity units per second. 7.5 matches the pre-iso camera's 120 px/s at PPU 16.")]
    [SerializeField] private float moveSpeedUnitsPerSecond = 7.5f;
    [SerializeField] private float sprintMultiplier = 2f;

    [Header("Placeholder Sprite")]
    [Tooltip("If null, a solid-colored capsule is generated at Start().")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color placeholderColor = new Color(0.86f, 0.30f, 0.30f, 1f);
    [SerializeField] private Color placeholderShadowColor = new Color(0.40f, 0.10f, 0.10f, 1f);
    [SerializeField] private Vector2Int placeholderSizePixels = new Vector2Int(24, 32);

    private InputActions inputActions;
    private InputAction moveAction;
    private InputAction sprintAction;
    private SpriteRenderer spriteRenderer;

    /// <summary>Float tile position; sub-tile precision so movement is smooth.</summary>
    public float WorldTileX { get; private set; }
    public float WorldTileY { get; private set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("PlayerController requires a SpriteRenderer component!");
        }

        inputActions = new InputActions();
        moveAction = inputActions.Player.Move;
        sprintAction = inputActions.Player.Sprint;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
    }

    private void Start()
    {
        if (sprite == null)
        {
            sprite = BuildCapsuleSprite(placeholderColor, placeholderShadowColor, placeholderSizePixels.x, placeholderSizePixels.y);
        }
        spriteRenderer.sprite = sprite;
        ApplyTransform();
    }

    private void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f) input.Normalize();

        float speed = moveSpeedUnitsPerSecond * (sprintAction.IsPressed() ? sprintMultiplier : 1f);
        float deltaUnityX = input.x * speed * Time.deltaTime;
        float deltaUnityY = input.y * speed * Time.deltaTime;

        // Screen-space delta → tile-space delta via inverse of IsoProjection.WorldToUnity.
        // WorldToUnity: unityX = (tileX - tileY), unityY = (tileX + tileY) * 0.5
        // Inverse:      tileX = unityX * 0.5 + unityY, tileY = unityY - unityX * 0.5
        WorldTileX += deltaUnityX * 0.5f + deltaUnityY;
        WorldTileY += deltaUnityY - deltaUnityX * 0.5f;

        ApplyTransform();
    }

    /// <summary>
    /// Teleport the character to the given tile. Camera catches up via deadzone follow.
    /// </summary>
    public void TeleportToTile(float tileX, float tileY)
    {
        WorldTileX = tileX;
        WorldTileY = tileY;
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        TerrainSample sample = TerrainService.SampleAt(Mathf.FloorToInt(WorldTileX), Mathf.FloorToInt(WorldTileY));

        // Same base-offset trick as MushroomInstance: sprite pivot is center, but
        // iso rendering wants the base at the tile point.
        float baseOffset = spriteRenderer.sprite != null
            ? spriteRenderer.sprite.rect.height * 0.5f / spriteRenderer.sprite.pixelsPerUnit
            : 0f;

        transform.position = IsoProjection.WorldToUnity(WorldTileX, WorldTileY, sample.height)
                           + new Vector3(0f, baseOffset, 0f);
        spriteRenderer.sortingOrder = IsoProjection.SortOrder(WorldTileX, WorldTileY, sample.height);
    }

    /// <summary>
    /// Build a solid-color capsule sprite at the requested pixel size, with a
    /// darker "shadow" stripe at the base so the character reads as standing on
    /// the tile rather than floating.
    /// </summary>
    private static Sprite BuildCapsuleSprite(Color body, Color shadow, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[w * h];
        Color transparent = new Color(0, 0, 0, 0);

        float cx = (w - 1) * 0.5f;
        float radius = w * 0.5f;
        int bodyTop = h - Mathf.RoundToInt(radius);
        int bodyBottom = Mathf.RoundToInt(radius);
        int shadowHeight = Mathf.Max(1, h / 12);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / radius;
                float dy;
                if (y >= bodyTop)                dy = (y - bodyTop) / radius;
                else if (y <= bodyBottom)        dy = (bodyBottom - y) / radius;
                else                             dy = 0f;

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > 1f)
                {
                    pixels[y * w + x] = transparent;
                }
                else if (y < shadowHeight)
                {
                    pixels[y * w + x] = shadow;
                }
                else
                {
                    pixels[y * w + x] = body;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            IsoProjection.PIXELS_PER_UNIT,
            0,
            SpriteMeshType.FullRect);
    }
}
