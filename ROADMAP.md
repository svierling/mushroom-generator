# Mushroom Generator - Development Roadmap

This document outlines the architectural plan for scaling the Mushroom Generator from a proof-of-concept to a full procedural sandbox exploration game.

---

## Executive Summary

The current architecture is **well-designed for its original scope** (3 fixed mushroom types) but has **structural limitations that block scaling** to thousands of procedural variations. The foundation is solid enough to refactor incrementally rather than rewriting.

---

## What's Working Well (Keep As-Is)

| Component | Why It's Good |
|-----------|---------------|
| **ProceduralRNG** | Excellent - stateful, deterministic, unlimited capacity for component generation |
| **Object Pooling** | Efficient - 100+ instances, zero GC after warmup, dynamic expansion |
| **Sector Coordinate System** | Perfect - infinite grid, negative coordinate handling solved |
| **SelectionManager Events** | Clean pattern - loose coupling between UI and game logic |
| **AudioManager Singleton** | Gold standard - replicate this pattern for new managers |
| **Input System** | Modern - using new InputSystem package correctly |

---

## Architectural Decisions (Confirmed)

### Sprite Composition: Multi-Layer SpriteRenderers
- Each mushroom has child objects: Cap, Stem (and later Details)
- Simple to implement, easy to debug
- Draw calls mitigated by texture atlas batching

### Color Variation: Grayscale + Tinting
- Create grayscale base sprites for caps and stems
- Apply color tint via `SpriteRenderer.color`
- 10 caps x 16 colors = only 10 art assets + 16 color definitions

### Naming System: Component-Based Names
- Combine word lists: `[cap_adjective] + [color_name] + [stem_name]`
- Example: "Spotted Red Bolete", "Frilled Golden Chanterelle"
- Infinite variety from manageable word lists

### World Seed System
- Each "generation" has a unique uint seed (random or user-provided)
- Seed combines with coordinates: `new ProceduralRNG(worldSeed, x, y)`
- Same seed + same coords = same mushroom (deterministic per world)
- Different seed = completely different world at (0,0)

---

## Proposed Refactoring Phases

### Phase 0.5: Isometric Perspective Overhaul (v1.0.5)
**Goal**: Convert the eagle's-eye 2D top-down world into a 2D isometric world centered on a main character sprite, with a follow-camera that uses a deadzone window and three discrete zoom levels. Procedural generation, RNG, and sector-coordinate math stay exactly as-is — this is a rendering + camera + input overhaul plus the addition of a player character.

**Note**: Terrain heights + slopes are **mechanically prepared for** in this phase — every tile carries a height value + slope type end-to-end from day one — but actual generation stays flat (all height = 0). Real terrain generation is Phase 0.75 below.

#### Design decisions (locked in)
- **Faked 2D isometric** with 2:1 tile ratio (not a tilted 3D camera). World math stays cartesian; iso transform applied at render time.
- **Camera locks to character with a deadzone window** (Metroid/Zelda/RCT-style). Character can roam inside the deadzone without moving the camera; camera only scrolls when the character pushes against the edge. Deadzone size scales with zoom so it feels the same at all zoom levels.
- **Smooth continuous movement** (not tile-locked hops). WASD moves the character, Shift sprints.
- **Ground tile grid** renders per sector using existing grass sprites.
- **Zoom levels**: 0.5x / 1x / 2x, cycled via mouse wheel or jumped via `[1]` `[2]` `[3]` keys.
- **Placeholder character**: solid colored capsule (~24x32 px) until a real sprite sheet exists.

#### Implementation sub-phases

**Sub-phase A — Projection + terrain-provider foundation (no visible change yet)**
1. Add `IsoProjection.cs` with `WorldToUnity(x, y, height)` / `UnityToWorld` / `HeightAtSubtile`. Constants: `TILE_WIDTH_PIXELS = 32`, `TILE_HEIGHT_PIXELS = 16`, `HEIGHT_STEP_PIXELS = 8`, `HEIGHT_SORT_WEIGHT`.
2. Add `TerrainSample` struct (`int height`, `SlopeType slope`), `SlopeType` enum (Flat + 4 edge + 4 corner slopes), `ITerrainProvider` interface, `FlatTerrainProvider` (returns `{ height: 0, slope: Flat }` for every coord), and a `TerrainService` singleton that consumers read from.
3. Update `MushroomInstance` to sample terrain height and use `IsoProjection.WorldToUnity(x, y, height)` for position; sort formula becomes `-((x + y) * 100 + height * HEIGHT_SORT_WEIGHT)`. Visible outcome: mushrooms shift to isometric arrangement (still flat, no character, no ground).

**Sub-phase B — Diamond visible region**
4. Update `MushroomGenerator` iteration to a diamond bounding box (currently rectangular). Iterate the rectangular bounding-box of the diamond in tile space and skip tiles that fall outside the camera rect after projection. Verify no pop-in at edges.

**Sub-phase C — Character + camera deadzone**
5. Create `PlayerCapsule.png` (~24x32), `Character.prefab`, `CharacterController.cs`. Character reads `Player.Move` / `Player.Sprint`, tracks float tile position, samples terrain height at position, projects to Unity world via `IsoProjection`.
6. Rewrite `CameraController.cs` from free-camera to follow-character-with-deadzone. Character owns `Player.Move` (no more WASD on the camera). Deadzone expressed in tiles (e.g. 5-tile half-width) and scales with zoom.

**Sub-phase D — Zoom**
7. Add `Player.Zoom` input action (mouse wheel Y axis + `1` / `2` / `3` keys).
8. Zoom adjusts `Camera.main.orthographicSize` between 0.5x / 1x / 2x and scales the deadzone proportionally. Persist last-selected zoom to `WorldManager.CurrentWorld` alongside camera position.

**Sub-phase E — Ground tiles**
9. `GroundTileGenerator` + `GroundTileInstance`, mirror of the mushroom pipeline. One tile per visible sector, sorted below mushrooms. Each tile samples `TerrainService.Provider` for its `TerrainSample` — bodies are correct for slopes even though the flat provider makes every tile flat today. Slope-variant sprite slots exist (e.g. `grass_slope_ne.png`) but fall back to `grass.png` until slope art lands.
10. Verify tiles cover visible diamond with no gaps at all zoom levels.

**Sub-phase F — Mouse picking + polish**
11. Update `MouseInteractionController` for iso screen→tile conversion (`ScreenToWorldPoint` then `IsoProjection.UnityToWorld`). Include a **height walk-down loop** (single iteration on flat terrain today; correct shape for slopes tomorrow — walks down from max possible height until it finds a tile whose projected Y matches the cursor).
12. Verify `HighlightRectangle` frames the hovered mushroom correctly at all zoom levels.
13. Update `CoordinateTrackerUI` / `CoordinateSearchUI` to display / navigate to the **character's** position instead of the camera center. Search teleports the character; camera catches up via deadzone follow.

**Sub-phase G — Height sanity check (still no generation)**
14. Temporarily swap `TerrainService.Provider` for a hand-coded `StaircaseTerrainProvider` returning `height = x / 4` (a 3-level staircase along the X axis). Verify tiles / mushrooms / character all shift up correctly, sort order stays consistent, mouse picking hits the topmost tile. **Revert to `FlatTerrainProvider` before merge** — this is a smoke test, not a feature.

#### New files
- `Assets/Scripts/Core/IsoProjection.cs`
- `Assets/Scripts/Core/TerrainSample.cs`
- `Assets/Scripts/Core/ITerrainProvider.cs`
- `Assets/Scripts/Core/FlatTerrainProvider.cs`
- `Assets/Scripts/Controllers/CharacterController.cs`
- `Assets/Scripts/Rendering/GroundTileGenerator.cs`
- `Assets/Scripts/Rendering/GroundTileInstance.cs`
- `Assets/Prefabs/Character.prefab`
- `Assets/Prefabs/GroundTile.prefab`
- `Assets/Sprites/Character/PlayerCapsule.png`

#### Files to modify
- `Assets/Scripts/Controllers/CameraController.cs` — free-camera → follow-character-with-deadzone; add zoom; persist zoom level
- `Assets/Scripts/Rendering/MushroomGenerator.cs` — diamond iteration, terrain sampling, iso projection
- `Assets/Scripts/Rendering/MushroomInstance.cs` — iso position + iso+height sort formula
- `Assets/Scripts/Controllers/MouseInteractionController.cs` — iso screen→tile with stubbed height walk-down
- `Assets/Scripts/UI/HighlightRectangle.cs` — verify highlight framing under iso projection
- `Assets/Settings/InputActions.inputactions` — add `Player.Zoom` action
- `Assets/Scenes/MainScene.unity` — add Character prefab instance at (0, 0), add GroundTileGenerator GameObject, wire CameraController → Character
- `Assets/Scripts/UI/CoordinateTrackerUI.cs` and `Assets/Scripts/UI/CoordinateSearchUI.cs` — display / navigate character position

#### Verification plan

*Sub-phase A/B:*
1. Existing mushrooms appear in isometric arrangement (diamond-shaped world)
2. Southeast-most mushrooms sort in front of northwest-most
3. No off-screen mushrooms visible; no gaps at camera edges when moving

*Sub-phase C:*
4. Character capsule appears at world (0, 0); WASD moves it smoothly; Shift sprints
5. Character can walk anywhere inside the central deadzone rectangle with the camera stationary
6. When the character crosses the deadzone edge, camera scrolls to keep the character on the edge — character never escapes the deadzone

*Sub-phase D:*
7. Mouse wheel cycles zoom through 0.5x → 1x → 2x with visible area scaling correctly
8. `1` / `2` / `3` jump directly to each zoom level
9. Deadzone feels consistent across zoom levels
10. Zoom level persists across scene reload

*Sub-phase E:*
11. Grass tiles cover the entire visible diamond with no gaps
12. Tiles sort behind mushrooms and character
13. No pool exhaustion / stutter at 2x zoom-out

*Sub-phase F:*
14. Hovering mouse over a mushroom highlights it correctly at all zoom levels
15. Clicking a mushroom opens its info window
16. `CoordinateTrackerUI` displays the character's current world tile
17. `CoordinateSearchUI` teleports the character (not the camera); camera catches up

*Sub-phase G (staircase smoke test, reverted before merge):*
18. With `StaircaseTerrainProvider`, tiles form three east-rising plateaus
19. Mushrooms on raised tiles sit visibly on top of their tiles
20. Cliff face tiles sort in front of the lower plateau behind them
21. Character walking east visibly steps up at plateau boundaries
22. Mouse cursor over a raised plateau selects the raised tile, not the flat tile behind it
23. **Revert `TerrainService.Provider` to `FlatTerrainProvider` before commit**

---

### Phase 0.75: Terrain Height Generation (v1.0.75)
**Goal**: Populate the terrain-provider plumbing established in Phase 0.5 with real height + slope generation, plus the art and animation work that raised terrain requires.

**Depends on**: Phase 0.5 (the `ITerrainProvider` interface, `TerrainSample` struct, height-aware sort formula, height walk-down mouse picker, and character height sampling are all already in place — this phase is a data-source swap plus art).

#### Scope
1. **`HeightmapTerrainProvider`** — Perlin- or simplex-noise-based `ITerrainProvider` with world-seed integration. Returns non-zero `height` and non-`Flat` `slope` where the noise dictates. Slope type derived from the four corner heights of each tile.
2. **Slope tile sprite set** — draw the 8 slope variants (4 edge + 4 corner) matching the reference image style, plus cliff/side-face sprites for the vertical walls between height levels.
3. **Slope-aware character movement** — bilinear-blend character height across slope tiles (fill in the flat-only `IsoProjection.HeightAtSubtile` body written in Phase 0.5). Character animation reacts to walking uphill / downhill.
4. **Slope-aware mushroom placement** — pick which corner of a slope tile the mushroom sits on (currently just uses the tile's average height).
5. **Optional pathing constraints** — decide whether the character can walk up any slope, or only slopes below a threshold steepness.

Files affected are limited to the terrain provider itself, the sprite folder, and small refinements in `MushroomInstance`, `CharacterController`, and `IsoProjection.HeightAtSubtile` — no architectural changes.

---

### Phase 1: World Seeds & Main Menu (v1.1.0)
**Goal**: Different "generations" with unique worlds, main menu for New/Load
**Note**: Works with current 3 mushroom types - no sprite changes needed

1. **Add World Seed to RNG**
   - Modify `ProceduralRNG` constructor to accept world seed
   - New seeding: `(worldSeed ^ x) << 16 | (worldSeed ^ y)` or similar
   - Same coordinates + different seed = different mushroom

2. **Create WorldManager (Singleton)**
   - Holds current world seed and world name
   - Generates random seed for new worlds
   - Provides seed to all generation systems

3. **Create Main Menu Scene**
   - "New Generation" button -> generates random seed, prompts for world name
   - "Load Generation" button -> shows list of saved worlds
   - "Continue" button -> loads last played world (if exists)
   - **Theme music**: Loop `Assets/Audio/Music/Mushroom Generator.ogg` while in menu
   - **Version display**: Show version number in top-left corner (e.g., "v1.1.0")

4. **Version Number System**
   - Store version in code constant (`GameVersion.cs`)
   - Display in top-left corner of ALL scenes (persists during gameplay too)
   - Phase 1 release = v1.1.0 (up from v1.0.0)
   - Stays visible until official release

5. **Main Menu Audio**
   - Play theme song on MainMenu scene load
   - Loop continuously while in menu
   - Stop when transitioning to gameplay (MainScene)

6. **Create WorldSaveData structure**
   ```csharp
   public class WorldSaveData {
       public string worldName;
       public uint worldSeed;
       public Vector2 lastCameraPosition;
       public DateTime lastPlayed;
   }
   ```

7. **Scene flow**
   - MainMenu scene -> MainScene (gameplay)
   - WorldManager persists across scenes (DontDestroyOnLoad)
   - Music stops on scene transition

**New files**:
- `Assets/Scripts/Managers/WorldManager.cs`
- `Assets/Scripts/Data/WorldSaveData.cs`
- `Assets/Scripts/Data/GameVersion.cs`
- `Assets/Scripts/UI/MainMenuUI.cs`
- `Assets/Scripts/UI/VersionDisplay.cs`
- `Assets/Scenes/MainMenu.unity`

**Files to modify**:
- `Assets/Scripts/Core/ProceduralRNG.cs` (add world seed parameter)
- `Assets/Scripts/Data/MushroomData.cs` (pass world seed to RNG)

---

### Phase 2: Component-Based Mushrooms
**Goal**: Enable component-based mushroom generation (caps, stems, colors) with rarity system
**Requires**: Art assets (10 grayscale caps, 10 grayscale stems)

1. **5-Tier Rarity System**
   ```
   1. Common      - ~50% spawn chance
   2. Uncommon    - ~30% spawn chance
   3. Rare        - ~15% spawn chance
   4. Very Rare   - ~4% spawn chance
   5. Anomaly     - ~1% spawn chance
   ```
   - Each cap, stem, AND color has its own rarity tier
   - RNG rolls against weighted probabilities to select components
   - Common components spawn frequently, Anomalies are extremely rare

2. **Combined Rarity Calculation**
   - Overall mushroom rarity = simple average of component rarities
   - Example: Common Cap (1) + Very Rare Stem (4) + Common Color (1) = (1+4+1)/3 = 2 -> Uncommon
   - Rounded to nearest tier
   - Makes finding all-rare combinations extremely exciting

3. **Replace MushroomType enum with component struct**
   ```csharp
   public struct MushroomData {
       public int capType;        // Index into caps array
       public int stemType;       // Index into stems array
       public int colorVariant;   // Index into colors array
       public Rarity capRarity;   // Rarity of the cap
       public Rarity stemRarity;  // Rarity of the stem
       public Rarity colorRarity; // Rarity of the color
       public Rarity overallRarity; // Calculated combined rarity
   }

   public enum Rarity { Common=1, Uncommon=2, Rare=3, VeryRare=4, Anomaly=5 }
   ```

4. **Create indexed sprite arrays with rarity metadata**
   ```csharp
   [System.Serializable]
   public class CapData {
       public Sprite sprite;
       public Rarity rarity;
       public string adjective; // e.g., "Spotted"
   }
   ```

5. **Weighted RNG for rarity-based selection**
   - Roll RNG -> get rarity tier based on weighted probability
   - Roll RNG again -> select random component within that tier

**Files to modify**:
- `Assets/Scripts/Data/MushroomData.cs`
- `Assets/Scripts/Data/MushroomSpriteData.cs`
- `Assets/Scripts/Rendering/MushroomGenerator.cs`

**New files**:
- `Assets/Scripts/Data/Rarity.cs` (enum + utility methods)

---

### Phase 3: Multi-Layer Rendering
**Goal**: Render composite mushrooms from parts with color tinting

1. **Update Mushroom.prefab**
   - Add child objects: CapRenderer, StemRenderer
   - Configure sorting layers

2. **Modify MushroomInstance**
   - Accept cap sprite + stem sprite + color
   - Configure both child renderers

3. **Implement color tinting**
   - Apply `SpriteRenderer.color` for variation

**Files to modify**:
- `Assets/Prefabs/Mushroom.prefab`
- `Assets/Scripts/Rendering/MushroomInstance.cs`

---

### Phase 4: Journal & Favorites
**Goal**: Save favorites and journal entries per world

1. **Create PersistenceManager**
   - JSON serialization to Application.persistentDataPath
   - Organizes saves by world name
   - Auto-save on quit, manual save option

2. **Create DiscoveryManager**
   - Track discovered mushrooms (per world)
   - Manage favorites list
   - Emit events for UI

3. **Extend WorldSaveData**
   - Include journal entries and favorites
   - Discovery timestamp, user notes per mushroom

**New files**:
- `Assets/Scripts/Managers/PersistenceManager.cs`
- `Assets/Scripts/Managers/DiscoveryManager.cs`
- `Assets/Scripts/Data/JournalEntry.cs`

---

### Phase 5: Terrain Foundation
**Goal**: Procedural terrain types affecting mushroom spawns

1. **Create TerrainData struct**
   - Types: Forest, Meadow, Swamp, Rocky, etc.
   - Uses world seed for determinism
   - Affects mushroom spawn rates/types per biome

2. **Create TerrainGenerator**
   - Larger-scale procedural generation (biomes span many sectors)
   - Noise-based for smooth transitions
   - Influences MushroomData generation

**New files**:
- `Assets/Scripts/Data/TerrainData.cs`
- `Assets/Scripts/Rendering/TerrainGenerator.cs`

---

### Phase 6: Future (NPCs, Advanced Features)
- NPC system with procedural personalities
- Tribes based on terrain/mushroom proximity
- Advanced UI (journal filtering, world maps)
- Multiple save slots per world

---

## Art Assets Required (Phase 2+3)

**Sprites**:
- 10 cap sprites (grayscale, white/light gray base for tinting)
- 10 stem sprites (grayscale, white/light gray base for tinting)
- Consistent anchor points: Caps should align to sit on stems
- File naming convention: `cap_01.png`, `stem_01.png`

**Rarity Assignments**:
- Assign each cap/stem/color a rarity tier (Common through Anomaly)
- Suggested distribution: ~5 Common, ~2 Uncommon, ~2 Rare, ~1 Very Rare, ~0-1 Anomaly

**Name Component Lists**:
- Cap adjectives (10): Spotted, Frilled, Smooth, Ridged, Wavy, Domed, Flat, Bulbous, Delicate, Layered
- Stem names (10): Bolete, Chanterelle, Parasol, Puffball, Morel, Inkcap, Amanita, Russula, Cortinarius, Lactarius
- Color names (16): Red, Orange, Yellow, Gold, Green, Teal, Blue, Purple, Pink, Brown, Tan, Gray, White, Cream, Black, Crimson

---

## Verification Plan

### After Phase 1 (World Seeds):
1. Launch game -> Main Menu appears, theme music plays and loops
2. Click "New Generation" -> enter world name -> game loads, music stops
3. Navigate to (0,0) and note which mushrooms exist
4. Return to Main Menu -> music resumes, create second world
5. Navigate to (0,0) in new world -> mushrooms should be DIFFERENT
6. Load first world -> mushrooms at (0,0) should be SAME as before
7. Quit and relaunch -> "Continue" loads last world correctly
8. Camera position persists between sessions

### After Phase 2+3 (Components):
1. Verify 10x10x16 = 1600 possible combinations generate correctly
2. Same coords + same seed = same mushroom (determinism)
3. Multi-layer rendering displays cap over stem correctly
4. Color tinting applies to grayscale sprites
5. Rarity distribution: ~50% Common, ~30% Uncommon, ~15% Rare, ~4% Very Rare, ~1% Anomaly
6. Combined rarity calculation works correctly
7. UI shows rarity with appropriate color
8. Performance test with 200+ visible mushrooms

---

## Estimated Scope

| Phase | Complexity | Files Changed/Added |
|-------|------------|---------------------|
| Phase 1: World Seeds | Medium | 6 new, 2 modified, 1 new scene |
| Phase 2: Components | Medium | 3 modified, 1 new |
| Phase 3: Multi-Layer | Medium | 2 modified, 1 prefab |
| Phase 4: Journal | Medium | 3 new files |
| Phase 5: Terrain | Medium-High | 2+ new files |
| Phase 6: NPCs | High | Multiple systems |

The refactoring is incremental - each phase delivers working functionality before the next begins.
