# Mushroom Generator - System Architecture

## Overview

This Unity project is a procedural mushroom generator migrated from a C++ olcPixelGameEngine application. It generates an infinite scrollable field where mushrooms spawn deterministically at specific coordinates based on seed-based procedural generation.

**Unity Version**: 6000.3.5f2
**Primary Packages**: Input System 1.17.0, URP 17.3.0, 2D Animation/Sprite Tools

## Core Systems

### 1. Procedural Generation System

**Primary File**: `Assets/Scripts/Data/MushroomData.cs`

The system uses a custom RNG algorithm that exactly replicates the C++ implementation:

```csharp
seed = (x & 0xFFFF) << 16 | (y & 0xFFFF)
```

**RNG Algorithm** (5-step hash process):
1. Increment seed by `0xe120fc15`
2. Apply multiplicative hashing with `0x4a39b70d`
3. XOR fold the result
4. Apply second hash with `0x12fad5c9`
5. XOR fold again

**Spawn Mechanics**:
- Spawn rate: ~1.43% (1 in 70 sectors)
- Three mushroom types:
  - **Bolete** (Red): 41.7% of mushrooms
  - **Roundhead** (Green): 50.0% of mushrooms
  - **Chanterelle** (Yellow): 16.7% of mushrooms (rarest)

**Critical Implementation Note**: Coordinates must be kept as `int` until passed to `MushroomData.Generate()` where they're cast to `uint`. Early casting causes negative coordinates to wrap to large positive values.

### 2. Rendering System

**Primary File**: `Assets/Scripts/Rendering/MushroomGenerator.cs`

**Architecture**: Per-frame regeneration with object pooling

The system regenerates all visible mushrooms every frame (matching C++ behavior) but uses object pooling to prevent garbage collection pressure:

```
Camera View → Calculate Visible Sectors → Return Pool Objects → Regenerate Visible → Activate from Pool
```

**Key Constants**:
- `SECTOR_SIZE_PIXELS = 16` (each sector is 16×16 pixels)
- `PIXELS_PER_UNIT = 16` (Unity sprite PPU setting)
- `SPRITE_OFFSET = -8` (centers 46px sprite in 16px sector)

**Coordinate Systems**:
1. **Sector coordinates**: Integer grid coordinates (seed input)
2. **Pixel coordinates**: World position in pixels
3. **Unity world units**: Rendering position (pixels ÷ 16)

**Position Calculation**:
```csharp
worldPixelX = (worldSectorX * 16) + (-8)
worldPixelY = (worldSectorY * 16) + (-8)
worldUnitsX = worldPixelX / 16
worldUnitsY = worldPixelY / 16
```

**Performance**:
- Initial pool size: 100 instances (configurable)
- Zero GC allocations per frame after warmup
- Supports 60 FPS with hundreds of visible mushrooms

### 3. Camera System

**Primary File**: `Assets/Scripts/Controllers/CameraController.cs`

**Movement**:
- Base speed: 120 pixels/second
- Sprint multiplier: 2.0× (activated by Shift key)
- Tracks camera offset in pixel coordinates for precision

**Coordinate Tracking**:
- Stores offset as `Vector2` in pixel space
- Converts to world units for camera positioning
- Provides public `CameraOffset` property for UI systems

### 4. Input System

**Primary File**: `Assets/Settings/InputActions.inputactions`

Uses Unity's new Input System (not legacy Input Manager):

**Action Maps**:
- **Player**: Movement (WASD), Sprint (Shift), Click (Mouse)
- **UI**: Submit (Enter/NumpadEnter), Navigate (Tab)

**Key Bindings**:
- `WASD`: Camera movement
- `Left/Right Shift`: Sprint (faster camera movement)
- `Left Mouse Button`: Select mushroom
- `Enter/Numpad Enter`: Submit coordinate search
- `Tab`: Toggle between X/Y input fields

### 5. Mouse Interaction System

**Primary File**: `Assets/Scripts/Controllers/MouseInteractionController.cs`

**Hover Detection**:
- Checks 3×3 sector grid around mouse position
- Accounts for sprite overlap (sprites are 46×53 pixels, larger than 16×16 sectors)
- Updates highlight rectangle position every frame

**Click Detection**:
- Only triggers when hovering over a mushroom
- Invokes `SelectionManager.SelectMushroom(x, y)`
- Plays audio feedback via `AudioManager`

### 6. UI System

#### Coordinate Search (`CoordinateSearchUI.cs`)
- Magnifying glass button toggles X/Y input fields
- Integer-only validation (no decimals)
- Enter key navigates camera to entered coordinates
- Tab key toggles focus between fields

#### Coordinate Tracker (`CoordinateTrackerUI.cs`)
- Bottom-left corner display
- Shows current sector coordinates in real-time
- Flashes green for 1.5 seconds after coordinate search navigation

#### Highlight Rectangle (`HighlightRectangle.cs`)
- Yellow border around hovered mushroom
- **Critical Feature**: Dynamic scaling with Canvas scale factor
- Dimensions: 56.7×77.3 world pixels (3.54×4.83 units)
- Maintains proportions when window resizes

**Scaling Formula**:
```csharp
screenPixelsPerWorldUnit = Screen.height / (orthographicSize * 2)
screenSize = worldSize * screenPixelsPerWorldUnit / canvas.scaleFactor
```

The division by `canvas.scaleFactor` prevents double-scaling when Unity's Canvas Scaler resizes UI elements.

### 7. Audio System

**Primary File**: `Assets/Scripts/Audio/AudioManager.cs`

**Architecture**: Singleton pattern

**Sound Effects**:
- `PlayMushroomClick()`: Triggered on mushroom selection
- `PlayUIClick()`: Triggered on UI button interactions

**Configuration**:
- Single `AudioSource` for all SFX
- Default volume: 0.7 (adjustable)
- Uses `PlayOneShot()` for non-overlapping playback

### 8. Selection Management

**Primary File**: `Assets/Scripts/Managers/SelectionManager.cs`

**State Tracking**:
- `isMushroomSelected`: Boolean selection state
- `selectedSeed1/selectedSeed2`: Sector coordinates
- `selectedMushroomData`: Full mushroom data

**Events** (for future UI expansion):
- `OnMushroomSelected`: Invoked with mushroom data
- `OnMushroomDeselected`: Invoked when selection cleared

## Component Hierarchy

```
GameManager (Root)
├── MushroomGenerator
│   └── Object Pool (100+ instances)
├── SelectionManager
└── AudioManager

Main Camera
├── CameraController (movement)
└── MouseInteractionController (hover/click)

UI Canvas
├── CoordinateSearchUI
│   ├── Search Button
│   └── Input Fields (X, Y)
├── CoordinateTrackerUI (bottom-left)
└── HighlightRectangle (yellow border)

MushroomContainer (Transform)
└── [Pooled mushroom instances]
```

## Data Flow

### Mushroom Spawning
```
Sector Coordinates (int x, int y)
  ↓
MushroomData.Generate(uint x, uint y)
  ↓
Custom RNG (5-step hash)
  ↓
MushroomData { exists, type, id }
  ↓
MushroomGenerator (if exists)
  ↓
Object Pool → MushroomInstance
  ↓
Configure(position, sprite)
  ↓
Rendered on screen
```

### User Interaction
```
Mouse Move
  ↓
MouseInteractionController.UpdateMousePosition()
  ↓
Check 3×3 sector grid
  ↓
MushroomData.Generate() for each sector
  ↓
If mushroom found → Show HighlightRectangle
  ↓
Mouse Click (if hovering)
  ↓
SelectionManager.SelectMushroom()
  ↓
AudioManager.PlayMushroomClick()
```

## Critical Implementation Details

### Negative Coordinate Handling

**Problem**: Casting negative sector coordinates to `uint` too early causes them to wrap to large positive values (e.g., -1 → 4294967295).

**Solution**: Keep coordinates as `int` until the final `Generate()` call:
```csharp
int worldSectorX = sectorOffsetX + screenX;  // Keep as int
MushroomData data = MushroomData.Generate((uint)worldSectorX, (uint)worldSectorY);
```

### Y-Sorting for Overlapping Mushrooms

**Problem**: Overlapping mushrooms render with incorrect depth ordering.

**Solution**: Dynamic sorting order based on Y position:
```csharp
spriteRenderer.sortingOrder = Mathf.RoundToInt(-position.y * 100f);
```

Lower Y positions (bottom of screen) have higher sorting order values → render in front.

### UI Scaling with Canvas

**Problem**: Fixed pixel-size highlights don't scale proportionally with world-space mushrooms when window resizes.

**Solution**: Calculate highlight size dynamically and compensate for Canvas scale factor:
```csharp
float screenPixelsPerWorldUnit = Screen.height / (mainCamera.orthographicSize * 2f);
float screenWidth = worldWidth * screenPixelsPerWorldUnit / canvas.scaleFactor;
```

Without dividing by `canvas.scaleFactor`, the Canvas Scaler's automatic UI scaling causes the highlight to shrink disproportionately.

## Performance Characteristics

**Target**: 60 FPS with smooth scrolling

**Optimization Strategies**:
1. **Object Pooling**: Pre-allocate 100 mushroom instances, expand as needed
2. **Per-Frame Regeneration**: Simple algorithm, no caching complexity
3. **Deterministic Generation**: No runtime RNG state, purely mathematical
4. **Efficient Hover Detection**: 3×3 grid check (9 sectors max) per frame
5. **Zero GC Allocations**: After warmup, no garbage collection pressure

**Profiling Results** (typical):
- MushroomGenerator.Update(): < 5ms per frame
- MouseInteractionController.Update(): < 1ms per frame
- Active mushrooms on screen: ~100-150 (1920×1080)
- Pool expansions: Rare after first minute of play

## Future Expansion Points

### Info Window System (Phase 4)
- Subscribe to `SelectionManager.OnMushroomSelected` event
- Display mushroom name, type, rarity, coordinates
- Add "Field Guide" button to catalog

### Advanced Features (Future Phases)
- Persistent collection tracking (PlayerPrefs or save file)
- Mushroom stats (size variations, growth patterns)
- Biome system (different areas = different mushroom types)
- Seasonal variations (time-based modulation)

## References

- Original C++ implementation: `migration_code/src/MushroomGenerator.cpp`
- Unity Input System documentation: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17
- Procedural generation algorithm: Custom RNG matching C++ exactly
