# Phase 1 Unity Setup Instructions

## What's Been Done (in VS Code)
✅ Created folder structure in Assets/
✅ Copied all sprites from migration_code/src/res/
✅ Copied audio files
✅ Implemented ProceduralRNG.cs (exact C++ match)
✅ Implemented MushroomData.cs (deterministic generation)
✅ Implemented CameraController.cs (WASD movement)
✅ Created RNGTest.cs (verification script)
✅ Created InputActions.inputactions (Input System configuration)

## What You Need to Do (in Unity Editor)

### Step 1: Configure Sprite Import Settings

**For each mushroom sprite, you need to set import settings:**

1. In Unity, navigate to `Assets/Sprites/Mushrooms/`
2. Select **MushroomRed.png**
3. In Inspector, set:
   - **Texture Type**: Sprite (2D and UI)
   - **Sprite Mode**: Single
   - **Pixels Per Unit**: **16** (CRITICAL for pixel-perfect rendering)
   - **Pivot**: Center
   - **Filter Mode**: Point (no filter) - for crisp pixel art
   - **Compression**: None
   - **Max Size**: 2048
   - **Alpha Source**: Input Texture Alpha
   - **Alpha Is Transparency**: ✓ (checked)
   - Click **Apply**

4. Repeat for:
   - MushroomGreen.png
   - MushroomYellow.png

5. For `Assets/Sprites/UI/MagnifyingGlass.png`, use the same settings

6. For `Assets/Sprites/Environment/` sprites (grass.png, etc.), use the same settings

---

### Step 2: Configure Audio Import Settings

**Background Music:**
1. Select `Assets/Audio/Music/overworld.wav`
2. In Inspector, set:
   - **Load Type**: Streaming (it's a large file)
   - **Preload Audio Data**: Unchecked
   - **Compression Format**: Vorbis
   - **Quality**: 70%
   - **Sample Rate Setting**: Preserve Sample Rate
   - Click **Apply**

**Sound Effect:**
1. Select `Assets/Audio/SFX/selectmushroom.wav`
2. In Inspector, set:
   - **Load Type**: Decompress On Load
   - **Preload Audio Data**: Checked
   - **Compression Format**: PCM
   - **Sample Rate Setting**: Preserve Sample Rate
   - Click **Apply**

---

### Step 3: Create MainScene

1. **Create New Scene:**
   - File → Save As...
   - Navigate to `Assets/Scenes/`
   - Name it "MainScene"
   - Save

2. **Configure Main Camera:**
   - Select "Main Camera" in Hierarchy
   - In Inspector, Camera component:
     - **Projection**: Orthographic
     - **Size**: **7.5** (shows 512x480 pixel area at 16 PPU)
     - **Clipping Planes**: Near 0.3, Far 1000
     - **Clear Flags**: Solid Color
     - **Background**: RGB(0, 100, 0) - Dark Green
       - Click the color picker
       - Set R=0, G=100, B=0, A=255
   - Position: (0, 0, -10)

3. **Add CameraController to Camera:**
   - With Main Camera selected
   - Click "Add Component"
   - Search for "Camera Controller"
   - Click to add the script
   - In Inspector, you should see:
     - Move Speed: 50 (leave as default)

4. **Create GameManager GameObject:**
   - Right-click in Hierarchy → Create Empty
   - Name it "GameManager"
   - Position: (0, 0, 0)

5. **Create MushroomContainer GameObject:**
   - Right-click in Hierarchy → Create Empty
   - Name it "MushroomContainer"
   - Position: (0, 0, 0)

---

### Step 4: Add RNG Test Script

1. **Attach RNGTest to GameManager:**
   - Select GameManager in Hierarchy
   - Click "Add Component"
   - Search for "RNG Test"
   - Click to add
   - In Inspector, verify:
     - Run On Start: ✓ (checked)
     - Detailed Output: ✓ (checked)

---

### Step 5: Test RNG and Camera Movement

1. **Save the scene** (Ctrl+S)

2. **Enter Play Mode** (Click Play button or Ctrl+P)

3. **Check Console for RNG Test Output:**
   - Open Console window (Window → General → Console)
   - You should see output like:
     ```
     === MUSHROOM GENERATOR RNG VERIFICATION TEST ===
     Sector (   0,   0): NO MUSHROOM
     Sector (  10,   5): NO MUSHROOM
     Sector ( 255, 255): NO MUSHROOM
     ...
     ```
   - Note which sectors have mushrooms and their types

4. **Test Camera Movement:**
   - While in Play mode, press WASD keys
   - In Scene view, you should see the camera moving
   - Watch the Transform position change in Inspector
   - The position updates based on the camera offset (offset / 16)

5. **Verify Camera Offset:**
   - While in Play mode
   - Select Main Camera
   - Expand CameraController component in Inspector
   - You should see "Camera Offset" updating as you press WASD
   - Offset should increase by ~50 units per second

6. **Exit Play Mode**

---

### Step 6: Compare RNG with C++ Version (Optional but Recommended)

To verify the RNG implementation matches C++ exactly:

1. **In VS Studio 2022**, open the C++ project:
   - Navigate to `migration_code/src/MushroomGenerator.cpp`
   - Add this test code to `main()` before the demo runs:
   ```cpp
   cValley test1(0, 0);
   printf("Sector (0,0): Exists=%d\n", test1.mushExists);

   cValley test2(10, 5);
   printf("Sector (10,5): Exists=%d\n", test2.mushExists);

   cValley test3(255, 255);
   printf("Sector (255,255): Exists=%d, Type2=%d, Type3=%d\n",
          test3.mushExists, test3.mushtype2, test3.mushtype3);
   ```

2. **Compile and run** the C++ program

3. **Compare output** with Unity Console
   - The "Exists" values should match exactly
   - The mushroom types should match

---

## Expected Results After Phase 1

✅ **Scene Setup:**
- MainScene created with dark green background
- Camera configured as Orthographic, size 7.5
- GameManager and MushroomContainer created

✅ **Camera Movement:**
- WASD keys move camera
- Camera offset updates correctly
- Camera position = offset / 16

✅ **RNG Verification:**
- RNGTest outputs results to Console
- Results should match C++ output for same coordinates
- Spawn rates approximately 1.43% (1 in 70)

✅ **Asset Configuration:**
- All sprites imported at 16 PPU with Point filter
- Audio files configured (Streaming for music, Decompress for SFX)

---

## Troubleshooting

### Problem: Sprites show as white squares
**Solution**:
- Check Sprite import settings
- Verify "Alpha Is Transparency" is checked
- Click "Apply" after changing settings

### Problem: Camera not moving with WASD
**Solution**:
- Verify CameraController is attached to Main Camera
- Check Console for any error messages
- Make sure you're in Play mode

### Problem: RNGTest not showing output
**Solution**:
- Check Console window is open (Window → General → Console)
- Verify RNGTest is attached to GameManager
- Check "Run On Start" is enabled

### Problem: Audio import errors
**Solution**:
- Unity might need time to import large audio files
- Check the progress bar at bottom-right of Unity
- Wait for import to complete

---

## Next Steps

Once Phase 1 is complete and tested:
1. Report back with:
   - RNG test results from Console
   - Screenshot of dark green background with camera moving
   - Any issues encountered

2. We'll proceed to **Phase 2**: Generation & Rendering
   - Implement MushroomGenerator.cs
   - Implement MushroomRenderer.cs
   - Display mushrooms on screen
   - Test deterministic generation

---

## Important Notes

- **Don't skip the sprite PPU setting!** 16 Pixels Per Unit is critical for matching the C++ version's 16-pixel sectors.

- **Save your work frequently** (Ctrl+S in Unity)

- **Test in Play mode**, not Edit mode. The scripts only run when playing.

- **Check the Console** for any errors or warnings. Red errors will prevent scripts from running.

- **Camera Z = -10** is important for 2D rendering. Sprites will be at Z=0.

Good luck with Phase 1 setup! Let me know when you're ready to move to Phase 2.
