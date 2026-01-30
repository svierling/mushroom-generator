# Development Notes & Troubleshooting

## Migration Overview

This project was migrated from a C++ olcPixelGameEngine application to Unity 6000.3.5f2. The migration preserved the exact behavior of the original implementation while adapting it to Unity's architecture.

**Original**: `migration_code/src/MushroomGenerator.cpp` (512×480 window, 2×2 pixel scale)
**Unity**: Flexible resolution, maintains same world generation logic

---

## Critical Issues Encountered & Solutions

### Issue 1: Input System Exception

**Problem**:
```
InvalidOperationException: You are trying to read input using the UnityEngine.Input class,
but you have switched active Input handling to Input System package in Player Settings.
```

**Root Cause**:
- Project configured with new Input System (`activeInputHandler: 1`)
- CameraController used legacy `Input.GetKey()` API
- C# class for InputActions asset wasn't generated

**Solution**:
1. In Unity, select `Assets/Settings/InputActions.inputactions`
2. In Inspector, check **"Generate C# Class"**
3. Set **"C# Class Name"** to `InputActions`
4. Click **"Apply"** to generate `InputActions.cs`
5. Updated CameraController to use new Input System:
```csharp
private InputActions inputActions;
private UnityEngine.InputSystem.InputAction moveAction;

void Awake() {
    inputActions = new InputActions();
    moveAction = inputActions.Player.Move;
}

void Update() {
    Vector2 movement = moveAction.ReadValue<Vector2>();
}
```

**Files Modified**:
- `Assets/Scripts/Controllers/CameraController.cs`
- `Assets/Settings/InputActions.inputactions` (generated C# class)

---

### Issue 2: Negative Coordinate Bug

**Problem**: Mushrooms don't render sprites at negative coordinates (X < 0 or Y < 0). Yellow highlight rectangles appear, but no sprites.

**Root Cause**:
Premature casting to `uint` caused negative values to wrap:
```csharp
// BROKEN CODE:
uint worldSectorX = (uint)(sectorOffsetX + screenX);  // -1 becomes 4294967295
MushroomData.Generate(worldSectorX, worldSectorY);    // Wrong seed!
```

**Technical Explanation**:
- Negative `int` values use two's complement representation
- Casting to `uint` reinterprets the bit pattern as a large positive number
- Example: `-1` (0xFFFFFFFF) → `4294967295` when cast to `uint`
- Different seeds produce different mushrooms (or no mushrooms)

**Solution**:
Keep coordinates as `int` until the final `Generate()` call:
```csharp
// FIXED CODE:
int worldSectorX = sectorOffsetX + screenX;  // Keep as signed int
int worldSectorY = sectorOffsetY + screenY;
MushroomData data = MushroomData.Generate((uint)worldSectorX, (uint)worldSectorY);
```

**Why This Works**:
- `MushroomData.Generate()` needs `uint` for the seed calculation (matching C++)
- Casting at the function call preserves the negative value's intended seed
- C++ conversion: `(uint32_t)-1 = 0xFFFFFFFF` (same as C#)

**Files Modified**:
- `Assets/Scripts/Rendering/MushroomGenerator.cs` (lines 98-103)

---

### Issue 3: Numpad Enter Not Working

**Problem**: Coordinate search only worked with standard Enter key, not numpad Enter.

**Root Cause**: InputActions only had one binding for `<Keyboard>/enter`.

**Solution**: Added second binding for numpad Enter:
```json
{
    "name": "",
    "id": "f3f3f3f3-0000-0000-0000-000000000001",
    "path": "<Keyboard>/numpadEnter",
    "action": "Submit"
}
```

**Files Modified**:
- `Assets/Settings/InputActions.inputactions` (added binding)

---

### Issue 4: Overlapping Mushrooms Depth Sorting

**Problem**: Overlapping mushrooms render with incorrect foreground/background perspective.

**Root Cause**: No sorting order assigned to mushroom sprites.

**Solution**: Implement Y-sorting in MushroomInstance:
```csharp
public void Configure(Vector3 position, Sprite sprite)
{
    transform.position = position;
    spriteRenderer.sprite = sprite;

    // Y-sorting: Lower Y = closer to viewer = render in front
    spriteRenderer.sortingOrder = Mathf.RoundToInt(-position.y * 100f);
}
```

**Why Multiply by 100**:
- Provides sufficient granularity for mushrooms close together
- Example: Mushrooms at Y=5.01 and Y=5.02 get orders -501 and -502
- Ensures clear separation even for nearby mushrooms

**Files Modified**:
- `Assets/Scripts/Rendering/MushroomInstance.cs` (Configure method)

---

### Issue 5: Highlight Rectangle Scaling Bug

**Problem**: Yellow highlight rectangle:
1. Initially had fixed size (200×260 pixels) that looked correct
2. When resizing Unity window, highlight shrank disproportionately to mushrooms
3. After fix attempt, highlight was too small

**Root Cause**: Canvas Scaler applies automatic UI scaling, causing "double-scaling":
```
My calculation: screenSize = worldSize * pixelsPerUnit
Canvas Scaler: actualSize = screenSize * scaleFactor
Result: Highlight shrinks too much when window shrinks
```

**Solution**: Compensate for Canvas scale factor:
```csharp
float screenPixelsPerWorldUnit = Screen.height / (mainCamera.orthographicSize * 2f);
float worldWidth = HIGHLIGHT_WIDTH_WORLD_PIXELS / PIXELS_PER_UNIT;
float screenWidth = worldWidth * screenPixelsPerWorldUnit;

// Critical: Divide by Canvas scale factor
if (canvas != null) {
    screenWidth /= canvas.scaleFactor;
    screenHeight /= canvas.scaleFactor;
}

rectTransform.sizeDelta = new Vector2(screenWidth, screenHeight);
```

**Scaling Factor Behavior**:
- Full screen (1920×1080): `scaleFactor = 1.0`
- Half screen (960×540): `scaleFactor = 0.5`
- Without compensation: Highlight gets scaled twice (our calc + Canvas)
- With compensation: Cancels out Canvas scaling, maintains proportions

**Final Dimensions**: 56.7×77.3 world pixels (3.54×4.83 world units)

**Files Modified**:
- `Assets/Scripts/UI/HighlightRectangle.cs` (Show method, added Canvas reference)

---

## Development Workflow Best Practices

### Input System Setup
1. Always check "Generate C# Class" for InputActions assets
2. Click "Apply" after any changes to regenerate the C# class
3. Never mix legacy Input Manager with new Input System
4. Use `OnEnable()`/`OnDisable()` to enable/disable actions

### Coordinate System Guidelines
1. Keep sector coordinates as `int` throughout calculations
2. Only cast to `uint` when calling `MushroomData.Generate()`
3. Document coordinate system conversions clearly
4. Test with negative coordinates early

### UI Scaling Considerations
1. For UI elements that match world-space objects:
   - Calculate size based on world units, not fixed pixels
   - Always compensate for Canvas scale factor
   - Test at multiple window sizes
2. Use `Screen.height` and `Camera.orthographicSize` for scaling
3. Avoid hard-coded pixel sizes for dynamic content

### Audio Integration
1. Use singleton pattern for AudioManager
2. Check `Instance != null` before calling
3. Use `PlayOneShot()` for overlapping sound effects
4. Keep volume configurable (inspector field)

### Object Pooling Strategy
1. Pre-warm pool in `Start()` (not `Awake()`)
2. Monitor pool expansion during play testing
3. Increase initial size if frequent expansions occur
4. Clear pools when changing scenes

---

## Common Pitfalls

### 1. Forgetting to Enable InputActions
```csharp
// Wrong - actions won't work:
inputActions = new InputActions();
moveAction = inputActions.Player.Move;

// Correct - must enable:
void OnEnable() {
    moveAction.Enable();
}
```

### 2. Reading Input in Wrong Method
```csharp
// Wrong - Input System uses events:
void Update() {
    if (submitAction.triggered) { }  // Won't work reliably
}

// Correct - subscribe to events:
void Awake() {
    submitAction.performed += OnSubmit;
}
```

### 3. Incorrect Sector Coordinate Calculation
```csharp
// Wrong - rounds instead of floors:
int sector = (int)(worldPos.x);  // -0.5 becomes 0

// Correct - always floor:
int sector = Mathf.FloorToInt(worldPos.x);  // -0.5 becomes -1
```

### 4. Sprite Sorting for 2D
```csharp
// Wrong - forgets to set sorting order:
spriteRenderer.sprite = mySprite;

// Correct - always set sorting for overlapping sprites:
spriteRenderer.sprite = mySprite;
spriteRenderer.sortingOrder = CalculateOrder(position.y);
```

---

## Testing Checklist

### Phase 1: Input & Camera
- [ ] WASD movement works
- [ ] Camera moves at 120 pixels/second
- [ ] Shift doubles movement speed
- [ ] Coordinate tracker updates in real-time
- [ ] Negative coordinates display correctly

### Phase 2: Generation & Rendering
- [ ] Mushrooms appear on screen
- [ ] Approximately 1.4% spawn density
- [ ] Three mushroom types visible
- [ ] Same pattern appears at same coordinates (deterministic)
- [ ] Smooth 60 FPS scrolling
- [ ] Mushrooms render at negative coordinates

### Phase 3: Mouse Interaction
- [ ] Yellow highlight appears over mushrooms
- [ ] Highlight follows mouse smoothly
- [ ] Highlight disappears when not over mushroom
- [ ] Left click selects mushroom
- [ ] Audio plays on mushroom click

### Phase 4: UI Features
- [ ] Coordinate search magnifying glass toggles input fields
- [ ] X and Y fields accept only integers (no decimals)
- [ ] Standard Enter and Numpad Enter both work
- [ ] Tab toggles focus between X and Y fields
- [ ] Camera jumps to entered coordinates
- [ ] Coordinate tracker flashes green after navigation
- [ ] UI click sound plays on button press

### Phase 5: Scaling & Polish
- [ ] Highlight maintains proportions when window resizes
- [ ] Mushrooms maintain proportions when window resizes
- [ ] Overlapping mushrooms show correct depth (Y-sorting)
- [ ] No console errors or warnings
- [ ] 60 FPS maintained at all window sizes

---

## Performance Profiling Tips

### Unity Profiler Settings
1. Open Profiler: Window → Analysis → Profiler
2. Focus on:
   - `MushroomGenerator.Update()` - Should be < 5ms
   - `MouseInteractionController.Update()` - Should be < 1ms
   - GC.Alloc - Should be 0 after warmup
3. Enable Deep Profile for detailed call stacks

### Expected Performance
- **Frame time**: 16.7ms target (60 FPS)
- **Active mushrooms**: 100-150 at 1920×1080
- **Object pool**: Rarely expands after first minute
- **Memory**: Stable after warmup (no leaks)

### Performance Red Flags
- Frame time > 20ms: Check visible sector calculation
- Pool expanding frequently: Increase initial size
- GC.Alloc > 0 after warmup: Check for allocations in Update()
- Stuttering on movement: Check camera movement smoothing

---

## Unity Version Compatibility

**Tested On**: Unity 6000.3.5f2

**Known Compatible Versions**:
- Unity 2022.3 LTS (with Input System 1.7.0+)
- Unity 6000.x series

**Package Dependencies**:
- Input System: 1.17.0 (required)
- Universal RP: 17.3.0 (optional, for post-processing)
- 2D Animation: 13.0.2 (optional, for sprite tools)

**Migration Notes for Other Unity Versions**:
- Unity 2021.x: May need Input System 1.4.0+ instead of 1.17.0
- Unity 2023.x: Should work without modifications
- WebGL: Requires Input System 1.7.0+ for proper browser support

---

## Future Development Recommendations

### Code Architecture
- Keep procedural generation logic separate from rendering
- Use events for loose coupling between systems
- Maintain single responsibility per component
- Document all coordinate system conversions

### Performance
- Profile regularly during development
- Test with large viewport sizes (4K, ultrawide)
- Monitor object pool growth over extended play sessions
- Consider spatial partitioning if sector count grows

### UI/UX
- Add visual feedback for all interactions
- Maintain consistent coordinate system displays
- Test input handling with various keyboard layouts
- Support controller input (future phase)

### Build Pipeline
- Set up build configurations for multiple platforms
- Include debug builds with profiling enabled
- Create automated testing for deterministic generation
- Version control all generated files (except Library/)

---

## Known Limitations

1. **No music system**: Background music was omitted (user request)
2. **No save system**: Mushroom collection not persistent yet
3. **No zoom controls**: Camera orthographic size is fixed
4. **2D only**: No 3D perspective or camera rotation
5. **Single biome**: All mushrooms use same generation rules
6. **No mobile support**: Touch input not implemented yet

---

## References

- Unity Input System Docs: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17
- C++ Implementation: `migration_code/src/MushroomGenerator.cpp`
- RNG Algorithm: Custom hash-based generation (documented in ARCHITECTURE.md)
