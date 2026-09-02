# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2D isometric mushroom explorer with per-save procedural worlds. Started as a C++ port that generated an infinite scrollable field of deterministic mushrooms; now evolving into a finite, plot-based sim (see Game Vision below).

- **Unity Version**: 6000.3.5f2
- **Language**: C#
- **Target Platform**: Windows 64-bit standalone

## Game Vision

Mushroom Generator is a **psychedelic-aquarium simulator meets exploration game**. The main character is **Natalia**, a plump Siberian cat, scurrying across a finite mushroom plot that she maintains. Each save file has its own seed (Minecraft-style) and grows into something unique over time.

The core hook: the plot is a living system where every variable — mushroom types, terrain factors, music notes, biomes, mobs, mob elements — combines with every other variable to produce emergent outcomes. Musical mushrooms, caves that unlock, new heights to climb, natural enemies that biomes spawn on their own. Content scales through **mathematical combinations** of a small set of primitives, not through hand-authoring new features per outcome.

**Design pillars**:
- **Combinatorial, not enumerative** — outcomes are math on existing variables (mushroom × terrain × music × mob element), not new code paths per feature. Sheer variable count, not sheer content count.
- **Finite plot per save** — worlds have a boundary. Large enough to explore, small enough to maintain and reason about. Coordinate-search teleports within the plot.
- **Care unlocks abilities** — caring for specific mushrooms/terrain/etc. rewards the player with special abilities (musical mushrooms, cave paths, elevation gain).
- **Reactive over time** — the plot reacts to every input like an aquarium; no two save files look alike after a few play sessions.

See `ROADMAP.md` for the phased build-out.

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

- `MushroomGenerator.cs` - Per-frame regeneration with object pooling. Uses `IsoProjection.WorldToUnity` for placement; sprites batch through `Mushrooms.spriteatlas`.
- `MushroomInstance.cs` - Individual renderer; iso sort order via `IsoProjection.SortOrder(x, y, height)` clamped to Int16 range.
- `GroundMeshRenderer.cs` - Single dynamic mesh for all visible ground tiles + scatter variation submesh.

**Controllers** (`Assets/Scripts/Controllers/`):

- `PlayerController.cs` - Natalia (character) at a float tile position; screen-relative WASD; shift-sprint (2x). Placeholder capsule sprite until Natalia art lands.
- `CameraController.cs` - Follow-camera with deadzone framing; three zoom levels (0.5x / 1x / 2x) via wheel or number keys.
- `MouseInteractionController.cs` - Iso screen→tile with height walk-down; hover detection scans a small tile neighborhood around the cursor.

**UI** (`Assets/Scripts/UI/`):

- `MainMenuUI.cs` - New/Load/Continue/Delete world flows in `MainMenu.unity`.
- `ReturnToMenuUI.cs` - Escape-to-menu with confirmation dialog.
- `CoordinateSearchUI.cs` - Search by coordinates; Enter/Numpad-Enter teleports the character.
- `CoordinateTrackerUI.cs` - Real-time tile coordinate display for the character.
- `HighlightRectangle.cs` - Yellow border overlay (must compensate for Canvas scale factor).

### Critical Implementation Details

1. **Coordinate Handling**: Keep tile coordinates as `int` until final cast to `uint` in `MushroomData.Generate()`. Negative coordinates wrap incorrectly if cast early.

2. **Input System**: Uses Unity's new Input System package (not legacy). Must generate C# class from `Assets/Settings/InputActions.inputactions`. Enable/disable actions in `OnEnable()`/`OnDisable()`.

3. **UI Scaling**: Highlight rectangle must compensate for Canvas scale factor: `size /= canvas.scaleFactor`

4. **RNG Determinism**: The 5-step hash algorithm must match C++ exactly for cross-platform consistency. `ProceduralRNG` accepts an `EntityNamespace` byte so mushrooms/trees/ores/mobs get independent streams at the same `(worldSeed, x, y)` — `Mushroom = 0` preserves bit-parity with pre-namespacing worlds.

5. **Object Pooling**: `MushroomGenerator` pre-warms 500 instances (worst-case 0.5x zoom). Zero GC allocations after warmup.

6. **Sprite Batching**: All mushroom sprites live in `Assets/Sprites/Mushrooms/Mushrooms.spriteatlas`. Register any new mushroom sprites here so they collapse into a single draw call at wide zooms.

7. **Save Format Versioning**: `WorldSaveData.schemaVersion` is set to `CURRENT_SCHEMA_VERSION` on new saves; older saves deserialize as `0`. Bump the constant and add a migration hook when the shape changes.

## Key Files

- `Assets/Scenes/MainScene.unity` - Main gameplay scene
- `Assets/Prefabs/Mushroom.prefab` - Base mushroom entity
- `Assets/Settings/InputActions.inputactions` - Input bindings configuration

## Existing Documentation

- `ARCHITECTURE.md` - Detailed system architecture and data flow
- `DEVELOPMENT.md` - Migration notes and troubleshooting guide
- `DEPLOYMENT.md` - Build and release procedures
- `ROADMAP.md` - Plans for the future (larger than bug fixes)
