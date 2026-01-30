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
