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
/// If walk/sprint spritesheets are assigned, they are sliced at Start() into
/// per-direction/per-frame Sprite arrays and driven from input direction +
/// sprint state each frame. If not, a runtime-generated capsule placeholder is
/// used. See CHARACTER_SPRITESHEETS.md for the expected sheet layout.
///
/// Named <c>PlayerController</c> (not <c>CharacterController</c>) to avoid
/// clashing with <see cref="UnityEngine.CharacterController"/>.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("On-screen movement speed in Unity units per second. Input is TILE-relative (grid-aligned): W walks +Y (up-left on screen), D walks +X (up-right on screen), W+D walks the tile diagonal (straight up on screen). Speed is scaled so the character moves at a constant on-screen pace regardless of direction. Solstice / early Ultima style — feet trace tile edges.")]
    [SerializeField] private float moveSpeedUnitsPerSecond = 6f;
    [SerializeField] private float sprintMultiplier = 2f;

    [Header("Spritesheets (see CHARACTER_SPRITESHEETS.md)")]
    [Tooltip("Walk sheet. 8 rows × walkColumns cols, each cell cellSize pixels.")]
    [SerializeField] private Texture2D walkSheet;
    [Tooltip("Sprint sheet. 8 rows × sprintColumns cols, same cell size as walk.")]
    [SerializeField] private Texture2D sprintSheet;
    [SerializeField] private Vector2Int cellSize    = new Vector2Int(48, 64);
    [SerializeField] private int walkColumns        = 6;
    [SerializeField] private int sprintColumns      = 8;
    [SerializeField] private float walkFps          = 10f;
    [SerializeField] private float sprintFps        = 16f;
    [Tooltip("Sprite pivot in normalized (0..1) coordinates. Y should be at the visible base of the character within the cell.")]
    [SerializeField] private Vector2 pivotNormalized = new Vector2(0.5f, 4f / 64f);

    [Header("Placeholder Capsule (used when sheets are unassigned)")]
    [SerializeField] private Color placeholderColor       = new Color(0.86f, 0.30f, 0.30f, 1f);
    [SerializeField] private Color placeholderShadowColor = new Color(0.40f, 0.10f, 0.10f, 1f);
    [SerializeField] private Vector2Int placeholderSizePixels = new Vector2Int(24, 32);

    private const int DIRECTION_COUNT = 8;
    // Facing indices — MUST match the row order in CHARACTER_SPRITESHEETS.md §4.
    private const int DIR_UP         = 0;
    private const int DIR_UP_RIGHT   = 1;
    private const int DIR_RIGHT      = 2;
    private const int DIR_DOWN_RIGHT = 3;
    private const int DIR_DOWN       = 4;
    private const int DIR_DOWN_LEFT  = 5;
    private const int DIR_LEFT       = 6;
    private const int DIR_UP_LEFT    = 7;

    private InputActions inputActions;
    private InputAction moveAction;
    private InputAction sprintAction;
    private SpriteRenderer spriteRenderer;

    private Sprite[,] walkFrames;   // [direction, frame]
    private Sprite[,] sprintFrames;
    private Sprite fallbackSprite;

    private int currentDirection = DIR_DOWN;
    private float animAccumulator;

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
        if (walkSheet != null)   walkFrames   = SliceSheet(walkSheet,   walkColumns);
        if (sprintSheet != null) sprintFrames = SliceSheet(sprintSheet, sprintColumns);

        if (walkFrames == null && sprintFrames == null)
        {
            fallbackSprite = BuildCapsuleSprite(placeholderColor, placeholderShadowColor, placeholderSizePixels.x, placeholderSizePixels.y);
            spriteRenderer.sprite = fallbackSprite;
        }

        ApplyTransform();
        ApplyCurrentSprite(isSprinting: false);
    }

    private void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f) input.Normalize();

        bool isSprinting = sprintAction.IsPressed();
        bool isMoving = input.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            currentDirection = DirectionFromInput(input);
            animAccumulator += Time.deltaTime * (isSprinting ? sprintFps : walkFps);
        }
        else
        {
            // Idle: freeze the animation on column 0 (per the spec, col 0 is a
            // full-stride pose that reads well as a standing frame).
            animAccumulator = 0f;
        }

        // Tile-relative: input.x drives +X tile axis, input.y drives +Y. Then
        // rescale the tile delta so its ON-SCREEN magnitude matches the desired
        // moveSpeed each frame — cardinals and diagonals both move at the same
        // visual pace, even though their tile-space magnitudes differ.
        float speed = moveSpeedUnitsPerSecond * (isSprinting ? sprintMultiplier : 1f);
        if (isMoving)
        {
            // Screen delta a single input-unit of tile input would produce.
            float screenDx = input.x - input.y;
            float screenDy = (input.x + input.y) * 0.5f;
            float screenMag = Mathf.Sqrt(screenDx * screenDx + screenDy * screenDy);
            if (screenMag > 0.0001f)
            {
                float scale = speed * Time.deltaTime / screenMag;
                WorldTileX += input.x * scale;
                WorldTileY += input.y * scale;
            }
        }

        ApplyTransform();
        ApplyCurrentSprite(isSprinting);
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

        // Character sprite's pivot is authored at its visible base, so placing
        // transform.position at the tile point drops the visible base onto the
        // tile. See MushroomInstance.Configure for the same convention.
        transform.position = IsoProjection.WorldToUnity(WorldTileX, WorldTileY, sample.height);
        spriteRenderer.sortingOrder = IsoProjection.SortOrder(WorldTileX, WorldTileY, sample.height);
    }

    private void ApplyCurrentSprite(bool isSprinting)
    {
        Sprite[,] frames = isSprinting && sprintFrames != null ? sprintFrames : walkFrames;
        if (frames == null)
        {
            // Fallback to the capsule if the caller hasn't wired sheets.
            if (fallbackSprite != null) spriteRenderer.sprite = fallbackSprite;
            return;
        }

        int cols = frames.GetLength(1);
        int frame = ((int)animAccumulator) % cols;
        Sprite chosen = frames[currentDirection, frame];
        if (chosen != null) spriteRenderer.sprite = chosen;
    }

    /// <summary>
    /// Slice a spritesheet Texture2D into an 8×cols Sprite array. Row 0 is the
    /// top row in image space (Unity textures store y=0 at the bottom, so we
    /// flip the row index).
    /// </summary>
    private Sprite[,] SliceSheet(Texture2D sheet, int columns)
    {
        Sprite[,] frames = new Sprite[DIRECTION_COUNT, columns];
        int cellW = cellSize.x;
        int cellH = cellSize.y;

        for (int row = 0; row < DIRECTION_COUNT; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int px = col * cellW;
                int py = sheet.height - (row + 1) * cellH;
                if (px + cellW > sheet.width || py < 0)
                {
                    // Sheet is smaller than the declared grid — leave slot null.
                    continue;
                }
                frames[row, col] = Sprite.Create(
                    sheet,
                    new Rect(px, py, cellW, cellH),
                    pivotNormalized,
                    IsoProjection.PIXELS_PER_UNIT,
                    0,
                    SpriteMeshType.FullRect);
            }
        }
        return frames;
    }

    /// <summary>
    /// Map a tile-space input vector to one of 8 screen-space facing indices.
    /// The character faces where the player sees them going, so we project the
    /// tile input through the iso transform first, then pick the sprite row.
    /// </summary>
    private static int DirectionFromInput(Vector2 input)
    {
        float screenX = input.x - input.y;
        float screenY = (input.x + input.y) * 0.5f;
        float angleDeg = Mathf.Atan2(screenY, screenX) * Mathf.Rad2Deg;
        int idx = Mathf.RoundToInt((90f - angleDeg) / 45f);
        return ((idx % DIRECTION_COUNT) + DIRECTION_COUNT) % DIRECTION_COUNT;
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
            new Vector2(0.5f, 0f),   // bottom-center pivot → sprite base sits on the tile
            IsoProjection.PIXELS_PER_UNIT,
            0,
            SpriteMeshType.FullRect);
    }
}
