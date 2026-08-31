# Character Spritesheets

Specification for the player-character sprite sheets. Hand this file to an
artist (or yourself) to produce the walking and sprinting animations for
every facing direction. Once the files land in `Assets/Sprites/Character/`
with the naming below and Unity import settings match the pivot section,
the existing rendering pipeline picks them up with no code changes.

---

## 1. What we need at a glance

| Mode | Directions | Frames per direction | Total sprites | One sheet size |
|---|---|---|---|---|
| Walk | 8 | 6 | 48 | 8 rows × 6 cols |
| Sprint | 8 | 8 | 64 | 8 rows × 8 cols |
| **Total** | | | **112** | 2 files |

Deliver as **two spritesheet PNGs**:

- `Assets/Sprites/Character/PlayerWalk.png` — 8 rows × 6 columns
- `Assets/Sprites/Character/PlayerSprint.png` — 8 rows × 8 columns

Row 0 is the top row of the image (screen-up). See §4 for the exact row → direction mapping.

---

## 2. Directions

Our iso projection is a 2:1 diamond (`IsoProjection.WorldToUnity`):
`unityX = tileX − tileY`, `unityY = (tileX + tileY) / 2`. WASD input is
**tile-relative** with **constant on-screen speed** (Solstice / early
Ultima style): `D` walks along the +X tile axis, `W` walks along +Y.
Cardinals map to iso-angle steps and diagonals map to screen cardinals,
but the code rescales the movement each frame so both feel like they
travel the same on-screen distance per second. Feet trace tile edges.

**Draw the character facing these 8 directions in screen space** — the
sprite rows correspond to on-screen facings, and the code picks the row
by projecting the tile input through the iso transform.

| # | Facing (screen) | On-screen motion | Which key(s) trigger this facing |
|---|---|---|---|
| 0 | Up | straight up | W + D (tile +X+Y) |
| 1 | Up-Right | 45° up-right | D (tile +X) |
| 2 | Right | straight right | S + D (tile +X−Y) |
| 3 | Down-Right | 45° down-right | S (tile −Y) |
| 4 | Down | straight down | S + A (tile −X−Y) |
| 5 | Down-Left | 45° down-left | A (tile −X) |
| 6 | Left | straight left | W + A (tile −X+Y) |
| 7 | Up-Left | 45° up-left | W (tile +Y) |

---

## 3. Sprite size and pivot

**Canvas size per frame: 48 × 64 pixels.**

- Wide enough to hold most poses without cropping the arms during walk cycles.
- Tall enough to be ~3× the tile height (tiles are 16 pixels tall in iso), which reads as a person-sized character on the grid.
- Same PPU as everything else in the project: **16 pixels per world unit**. So the character is `(48/16, 64/16) = (3.0, 4.0)` world units on screen.

**Pivot: bottom-center of the *visible* base of the character** (not the
sprite rect corner). Same convention as the mushroom sprites — the
pivot IS the anchor at the tile, no code offset is applied.

Concretely, in Unity's normalized pivot coordinates:

- `spritePivot.x = 0.5` (horizontal center)
- `spritePivot.y = (visible-base-pixel-y) / 64` (where the character's feet touch the ground within the 48×64 canvas)

For example, if the character's feet sit at pixel row 4 from the bottom of the 64-pixel canvas (leaving 4 pixels of shadow/blur room below), `spritePivot.y = 4 / 64 = 0.0625`.

The `.meta` import settings should end up with:

```
alignment: 9              # Custom
spritePivot: {x: 0.5, y: <fraction described above>}
spritePixelsToUnits: 16
```

---

## 4. Spritesheet layout

Both `PlayerWalk.png` and `PlayerSprint.png` use the same 8-row grid, one row per direction. Column count differs by mode.

```
                col 0    col 1    col 2    col 3    col 4    col 5   (col 6)  (col 7)
row 0  Up       ┌──────┬──────┬──────┬──────┬──────┬──────┬──────┬──────┐
row 1  Up-Right │      │      │      │      │      │      │      │      │
row 2  Right    ├──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤
row 3  Down-Right│     │      │      │      │      │      │      │      │
row 4  Down     ├──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤
row 5  Down-Left│      │      │      │      │      │      │      │      │
row 6  Left     ├──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤
row 7  Up-Left  │      │      │      │      │      │      │      │      │
                └──────┴──────┴──────┴──────┴──────┴──────┴──────┴──────┘
                └────────── walk: 6 cols ─────────┘
                └──────────────────── sprint: 8 cols ────────────────────┘
```

- `PlayerWalk.png`: **6 columns × 8 rows = 288 × 512 pixels** total
- `PlayerSprint.png`: **8 columns × 8 rows = 384 × 512 pixels** total

Row 0 sits at the **top** of the image; row 7 is at the bottom. This
matches Unity's Sprite Editor when it splits by cell size — the topmost
row will be sprite index 0.

### Row → direction lookup (paste into your art app)

```
Row 0 → Up (screen north)
Row 1 → Up-Right
Row 2 → Right (screen east)
Row 3 → Down-Right
Row 4 → Down (screen south)
Row 5 → Down-Left
Row 6 → Left (screen west)
Row 7 → Up-Left
```

### Column ordering (both modes)

Frames read left to right; the loop plays `col 0 → col N-1 → col 0 …` in
sequence. **Column 0 should be a full-stride pose, not a rest pose** —
this way when the character is standing still and Unity shows only the
first frame, it looks natural rather than mid-step. Alternate feet
between adjacent frames so the walk cycle reads correctly.

Walk cycle (6 frames): left foot stride → left-foot contact → both-feet
pass → right foot stride → right-foot contact → both-feet pass.

Sprint cycle (8 frames): similar to walk but with more air time and a
bigger stride length. Extra 2 frames give room for a "lean forward" pose
in the pass frames.

---

## 5. Naming (final files)

Only two PNGs live in the repo:

```
Assets/Sprites/Character/PlayerWalk.png
Assets/Sprites/Character/PlayerSprint.png
```

When you import in Unity, use **Sprite Mode: Multiple** and slice by
**Grid By Cell Size** with cell size 48 × 64. Unity will auto-generate
sub-sprites named `PlayerWalk_0`, `PlayerWalk_1`, … up to
`PlayerWalk_47` (row-major, top-left first).

Any future animation states (idle, jump, etc.) should live in their own
PNGs following the same convention:
`Assets/Sprites/Character/Player<StateName>.png`.

---

## 6. Style guide

Match the existing mushroom sprites so the character looks like it belongs on the same map:

- **Pixel-perfect at 16 PPU.** No anti-aliasing on the outlines; use full-alpha pixels only. (Look at `Assets/Sprites/Mushrooms/MushroomRed.png` for the reference silhouette style.)
- **Bold saturated colors** with 1–2 shadow tones per color. Avoid gradients.
- **Outline the character in a 1-pixel darker shade** on the shaded side to help it read against the green tiles.
- **Shadow directionality:** treat the light source as coming from the top-left of the screen. The character's right side (screen-right) is in shadow.
- **No transparent padding** on the sides of the sprite unless you need it for arms swinging. Bottom padding (2–5 pixels) is fine and is where the pivot lives.

---

## 7. Animation timing (for reference — not baked into the sheets)

Sprites are static images; the timing is controlled by the code / Animator later, but plan the poses assuming:

- Walk: **10 fps** → full 6-frame cycle plays in 0.6 s
- Sprint: **16 fps** → full 8-frame cycle plays in 0.5 s

At the default 7.5 units/sec walk speed and 15 units/sec sprint speed
(defined in `PlayerController`), the character crosses roughly one tile
per walk cycle, which lands the "feet contact" frames on tile crossings.

---

## 8. Deliverable checklist

- [ ] `PlayerWalk.png` — 288 × 512 pixels, 48 sprites in 8 rows × 6 cols
- [ ] `PlayerSprint.png` — 384 × 512 pixels, 64 sprites in 8 rows × 8 cols
- [ ] Both PNGs use pixel-perfect art at 16 PPU
- [ ] Pivot for both sprites sits at `(0.5, visible-base-pixel / 64)`
- [ ] Directions ordered per §4: rows Up, Up-Right, Right, Down-Right, Down, Down-Left, Left, Up-Left
- [ ] Column 0 is a full-stride pose (used as the idle frame until an idle animation exists)
- [ ] Style matches the mushroom reference in §6

---

## 9. What the code expects (informational)

Wiring is already implemented (`PlayerController` slices the sheets at
Start and drives the animation each frame):

1. `Player.Move` is interpreted as **tile-space** input. The character
   moves along tile axes (see §2 for the mapping).
2. The tile-space input is projected through the iso transform to find
   the on-screen direction of travel, which selects the sprite row.
   Idle preserves the last non-zero facing.
3. Walk vs sprint is chosen from `Player.Sprint`.
4. The column advances at the fps in §7 and wraps at the end of the
   row. Idle freezes on column 0.

Everything downstream — iso projection, sort order, camera follow,
mouse picking — already works with any sprite that respects the
pivot-at-visible-base convention. No changes needed to
`IsoProjection`, `CameraController`, or `MouseInteractionController`
once the sprites are wired to the SpriteRenderer.
