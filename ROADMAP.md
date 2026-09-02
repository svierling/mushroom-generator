# Mushroom Generator - Development Roadmap

This document outlines the architectural plan for scaling the Mushroom Generator from a proof-of-concept to a full procedural sandbox exploration game.

---

## Executive Summary

The Phase 0.5 isometric overhaul and Phase 1 world/main-menu work have shipped (v1.2.0); the game is a playable isometric explorer with save/load and world seeds. Recent foundation-optimizations work landed sprite atlasing, sort-order safety, RNG entity namespacing, tile-terminology cleanup, and save schema versioning.

**Next up**: shape the game into its design vision (see "Game Vision" in `CLAUDE.md`) — a **finite mushroom plot per save**, maintained by **Natalia** (a plump Siberian cat), where gameplay emerges combinatorially from mushroom × terrain × music × mob interactions. Immediate roadmap priorities: world boundary + Natalia sprite (Phase 1.5), real terrain generation (Phase 0.75), 1600-combo component-based mushrooms (Phase 2 — data shape is prepped), and biome regions (Phase 5).

---

## What's Working Well (Keep As-Is)

| Component | Why It's Good |
|-----------|---------------|
| **ProceduralRNG** | Stateful, deterministic, unlimited capacity for component generation. Now namespaced by `EntityNamespace` so trees/ores/mobs get independent streams. |
| **Object Pooling** | 500-instance pool pre-warmed for 0.5x-zoom worst case, zero GC after warmup, dynamic expansion. |
| **Tile Coordinate System** | Infinite integer grid, negative coordinates safe (RNG uses XOR-based hash). |
| **Isometric Projection** | 2:1 dimetric via `IsoProjection`, matches pikuma-standard `(x-y, x+y)` math. Sort order clamped to Int16 range for safety at extreme coords. |
| **SelectionManager Events** | Loose coupling between UI and game logic, scene-scoped singleton guard. |
| **AudioManager Singleton** | Gold standard — replicate this pattern for new managers. |
| **Input System** | New InputSystem package, screen-relative WASD, sprint, mouse picking with height walk-down. |
| **Sprite Atlas** | `Mushrooms.spriteatlas` collapses visible mushrooms into a single draw call. Register new sprites here. |

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

### Phase 0.5: Isometric Perspective Overhaul (v1.0.5) — SHIPPED

Converted the top-down world to 2:1 isometric with a follow-camera, character sprite (Landstalker-style screen-relative WASD + sprint), three discrete zoom levels, deadzone camera framing, mesh-based ground tiles, iso mouse picking with a height walk-down, and terrain-provider plumbing that carries height + slope through the whole pipeline (all flat today, but ready for Phase 0.75).

Key modules landed: `IsoProjection`, `TerrainSample`, `ITerrainProvider` / `TerrainService` / `FlatTerrainProvider`, `StaircaseTerrainProvider` (test util), `PlayerController`, `GroundMeshRenderer`, plus iso updates to `MushroomGenerator`, `MushroomInstance`, `MouseInteractionController`, `CameraController`, coord UI. See git history for sub-phase commits.

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

### Phase 1: World Seeds & Main Menu (v1.1.0) — SHIPPED

Each world has a random `uint` seed persisted in `WorldSaveData` (now with a `schemaVersion` field for future migrations). `MainMenu.unity` + `MainMenuUI` + `ReturnToMenuUI` handle New/Load/Continue/Delete flows and theme-music playback; `WorldManager` (`DontDestroyOnLoad` singleton) persists across scenes; version display and Escape-to-menu are wired.

---

### Phase 1.5: World Boundary & Natalia
**Goal**: Convert the infinite scrollable field into a finite, per-save **mushroom plot**. Introduce **Natalia** — a plump Siberian cat — as the player character replacing the placeholder capsule. This is the phase that turns the tech demo into the game described in "Game Vision" (`CLAUDE.md`).

**Why finite plots**: an infinite world is expensive to reason about and maintain — for both the engine and the player. A bounded plot is large enough to explore fully over many sessions, small enough that every mushroom, terrain change, and cave feels intentional. It also gives biomes, terrain, and care mechanics natural containment: Phase 5 biomes tile inside the plot, Phase 0.75 terrain has a knowable maximum extent, and player agency (Phase 2+ care mechanics) has a canvas to react to.

#### Scope
1. **`WorldBounds`** — plot dimensions in tiles baked into `WorldSaveData` at world creation. Deterministic from seed. Target range TBD, likely 256×256 to 512×512 tiles (large enough for meaningful exploration; small enough that the whole plot fits in memory and can react to player input). Save-format bump: `WorldSaveData.CURRENT_SCHEMA_VERSION` -> 2 with a migration that assigns a default plot size to pre-existing worlds.
2. **Movement clamp** — `PlayerController` prevents movement outside the plot boundary; smooth stop at the edge (not a hard wall bounce).
3. **Camera clamp** — `CameraController` deadzone respects plot bounds; camera can't scroll to reveal off-plot area (letterbox / matte the outside if needed).
4. **Culling** — `MushroomGenerator` and `GroundMeshRenderer` skip tiles outside the plot. Edge indicator (fence, cliff, mist, or "you are approaching the plot boundary" HUD ping) so the player understands the boundary.
5. **Mouse picking & search** — `MouseInteractionController` rejects clicks outside the plot; `CoordinateSearchUI` rejects out-of-range coordinates and clamps teleport destinations to the plot.
6. **Natalia sprite** — Siberian cat character sprite (walk/sprint, 8-directional, per the existing `PlayerController` sheet layout). Placeholder capsule stays as the fallback until art lands.
7. **New-world flow** — main menu "New Generation" doesn't need to expose plot size to the player yet; use a fixed default. If we later want variable-size plots, expose a slider or preset picker in the New World panel.

**Files to modify**:
- `Assets/Scripts/Data/WorldSaveData.cs` — add plot dimensions + migrate
- `Assets/Scripts/Managers/WorldManager.cs` — accessor for plot bounds
- `Assets/Scripts/Controllers/PlayerController.cs` — movement clamp
- `Assets/Scripts/Controllers/CameraController.cs` — camera clamp
- `Assets/Scripts/Rendering/MushroomGenerator.cs` — cull off-plot tiles
- `Assets/Scripts/Rendering/GroundMeshRenderer.cs` — cull off-plot tiles + edge visualization
- `Assets/Scripts/Controllers/MouseInteractionController.cs` — reject off-plot clicks
- `Assets/Scripts/UI/CoordinateSearchUI.cs` — clamp teleport to plot

**New files** (art dependent):
- `Assets/Sprites/Character/NataliaWalk.png`, `NataliaSprint.png` (or procedural stopgap)

---

### Phase 2: Component-Based Mushrooms
**Goal**: Enable component-based mushroom generation (caps, stems, colors) with rarity system
**Requires**: Art assets (10 grayscale caps, 10 grayscale stems)

**Prep landed** (feature/mushroom-component-shape, 2026-09-02): `MushroomData` now carries `capIndex/stemIndex/colorIndex/capRarity/stemRarity/colorRarity/overallRarity`; `Rarity` enum defined (`Common=1..Anomaly=5`); `MushroomPresets` maps the 3 legacy types to preset component tuples so today's mushrooms populate the new fields with identical UI output. `MushroomSpriteData.GetSprite(MushroomData)` overload picks sprites by component (falls back to type for anything outside the shipped presets). `EntityNamespace.Mushroom = 0` in `ProceduralRNG` guarantees that adding rarity rolls in Phase 2 won't shift RNG streams for future entity kinds.

**What Phase 2 still needs**:
1. **Art assets** — 10 cap + 10 stem grayscale sprites (see "Art Assets Required" below), registered as `Sprite[]` arrays on `MushroomSpriteData`.
2. **16-color palette** — colour definitions + `SpriteRenderer.color` tint applied per instance.
3. **Rarity-driven rolls** — rewrite `MushroomData.Generate` to roll cap/stem/color/rarity components directly. Weighted tier probabilities: Common ~50%, Uncommon ~30%, Rare ~15%, Very Rare ~4%, Anomaly ~1%. Overall rarity = round(average of component rarities).
4. **Name generator** — combine `[cap_adjective] + [color_name] + [stem_name]` word lists (see "Art Assets Required" below for suggested lists).
5. **UI wiring** — update `MushroomData.GetName()`, `GetRarity()`, `GetRarityColor()` to read from component fields instead of the legacy `MushroomType` switch. `InfoWindowUI` and rarity-color mapping follow.

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

### Phase 5: Biomes / Terrain Types
**Goal**: Procedural biome regions affecting mushroom spawns, foliage, ambient art. Coordinates with Phase 0.75 — biome type can drive terrain-height parameters (rolling hills for "Pennsylvania-style" temperate biome, flat for plains, etc.).

1. **Create `BiomeData` / `IBiomeProvider`**
   - Types: Forest, Meadow, Swamp, Rocky, etc.
   - Deterministic from world seed (Voronoi cells or noise-based regions)
   - Named "namespaces" independent from `EntityNamespace` (biome selects RNG streams for spawn rolls)

2. **Biome-driven mushroom mix**
   - Per-biome spawn-rate multiplier + component-weight overrides
   - Border blending (partial spawn probability at biome edges)

3. **Optional biome-driven terrain profile**
   - Each biome supplies `HeightmapTerrainProvider` parameters (amplitude, frequency, octaves)
   - Terrain provider composes biome sample + noise to produce final tile height

**New files**:
- `Assets/Scripts/Data/BiomeData.cs`
- `Assets/Scripts/Core/IBiomeProvider.cs`
- `Assets/Scripts/Rendering/BiomeGenerator.cs` (if visualization needed)

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

| Phase | Complexity | Status |
|-------|------------|--------|
| Phase 0.5: Isometric Overhaul | Medium | Shipped |
| Phase 1: World Seeds & Main Menu | Medium | Shipped |
| Phase 1.5: World Boundary & Natalia | Medium | Pending — game-defining scope decision |
| Phase 0.75: Terrain Height Generation | Medium-High | Pending — depends on slope art |
| Phase 2: Component-Based Mushrooms | Medium | Data shape prepped; needs art + rarity rolls |
| Phase 3: Multi-Layer Rendering | Medium | Pending — depends on Phase 2 |
| Phase 4: Journal & Favorites | Medium | Pending |
| Phase 5: Biomes / Terrain Types | Medium-High | Pending — coordinate with Phase 0.75 |
| Phase 6: NPCs & Advanced Features | High | Pending |

Phases are incremental — each delivers working functionality before the next begins.
