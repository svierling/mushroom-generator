# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2D procedural mushroom field generator migrated from C++. Generates an infinite scrollable field of deterministic mushrooms using a hash-based RNG algorithm.

- **Unity Version**: 6000.3.5f2
- **Language**: C#
- **Target Platform**: Windows 64-bit standalone

## Build Commands

This is a Unity project - all build operations use Unity Editor:

- **Open Project**: Open Unity Hub, add project folder, open with Unity 6000.3.5f2
- **Build**: File > Build Profiles > Windows > Build
- **Play in Editor**: Press Play button or Ctrl+P in Unity Editor
- **Run Tests**: Window > General > Test Runner

## Architecture

### Core Systems

**Procedural Generation** (`Assets/Scripts/Core/` & `Assets/Scripts/Data/`):

- `ProceduralRNG.cs` - 5-step hash-based RNG matching C++ implementation exactly. Uses magic constants `0xe120fc15`, `0x4a39b70d`, `0x12fad5c9`
- `MushroomData.cs` - Mushroom struct with `Generate(uint x, uint y)` method. ~1.43% spawn rate, three types: Bolete (41.7%), Roundhead (50%), Chanterelle (16.7%)

**Rendering** (`Assets/Scripts/Rendering/`):

- `MushroomGenerator.cs` - Per-frame regeneration with object pooling. Constants: `SECTOR_SIZE_PIXELS = 16`, `PIXELS_PER_UNIT = 16`
- `MushroomInstance.cs` - Individual renderer with Y-sorting: `sortingOrder = -position.y * 100f`

**Controllers** (`Assets/Scripts/Controllers/`):

- `CameraController.cs` - WASD movement at 120 pixels/second, 2x sprint multiplier (Shift)
- `MouseInteractionController.cs` - Hover detection on 3x3 sector grid, click selection

**UI** (`Assets/Scripts/UI/`):

- `CoordinateSearchUI.cs` - Search by coordinates with Enter/Numpad Enter navigation
- `CoordinateTrackerUI.cs` - Real-time sector coordinate display
- `HighlightRectangle.cs` - Yellow border overlay (must compensate for Canvas scale factor)

### Critical Implementation Details

1. **Coordinate Handling**: Keep sector coordinates as `int` until final cast to `uint` in `MushroomData.Generate()`. Negative coordinates wrap incorrectly if cast early.

2. **Input System**: Uses Unity's new Input System package (not legacy). Must generate C# class from `Assets/Settings/InputActions.inputactions`. Enable/disable actions in `OnEnable()`/`OnDisable()`.

3. **UI Scaling**: Highlight rectangle must compensate for Canvas scale factor: `size /= canvas.scaleFactor`

4. **RNG Determinism**: The 5-step hash algorithm must match C++ exactly for cross-platform consistency.

5. **Object Pooling**: `MushroomGenerator` uses pool of 100 initial instances. Zero GC allocations after warmup.

## Key Files

- `Assets/Scenes/MainScene.unity` - Main gameplay scene
- `Assets/Prefabs/Mushroom.prefab` - Base mushroom entity
- `Assets/Settings/InputActions.inputactions` - Input bindings configuration

## Existing Documentation

- `ARCHITECTURE.md` - Detailed system architecture and data flow
- `DEVELOPMENT.md` - Migration notes and troubleshooting guide
- `DEPLOYMENT.md` - Build and release procedures
- `ROADMAP.md` - Plans for the future (larger than bug fixes)
