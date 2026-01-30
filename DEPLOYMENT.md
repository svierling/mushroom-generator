# Deployment Plan - Mushroom Generator

## Overview

This document outlines the process for building and deploying the Mushroom Generator as a lightweight, standalone Windows executable for easy testing across devices.

**Target**: Windows 64-bit standalone executable
**Distribution**: GitHub Releases (zipped download)
**File Size Goal**: < 50 MB (lightweight and portable)

---

## Build Configuration

### Unity Build Settings

1. **Open Build Settings**
   - In Unity: File → Build Settings (Ctrl+Shift+B)

2. **Platform Selection**
   - Select **PC, Mac & Linux Standalone**
   - Set Target Platform: **Windows**
   - Set Architecture: **x86_64** (64-bit)

3. **Compression Settings**
   - Compression Method: **LZ4** (faster startup) or **LZ4HC** (smaller size)
   - Recommended: **LZ4** for quick testing builds

4. **Development Build Options** (for testing)
   - ☐ Development Build (uncheck for release)
   - ☐ Autoconnect Profiler (uncheck for release)
   - ☐ Deep Profiling Support (uncheck for release)
   - ☐ Script Debugging (uncheck for release)

5. **Player Settings** (Important)
   - Click "Player Settings..." button
   - Navigate to sections below

### Player Settings Configuration

#### **Company and Product**
- Company Name: (Your name/studio)
- Product Name: **Mushroom Generator**
- Version: **1.0.0** (increment for each release)

#### **Icon** (Optional but recommended)
- Default Icon: Set to mushroom sprite icon (if available)
- Location: `Assets/Sprites/MushroomRed.png` (export as PNG at 512×512 for icon)

#### **Resolution and Presentation**
- Fullscreen Mode: **Windowed** (allows resizing)
- Default Screen Width: **1280**
- Default Screen Height: **720**
- Run In Background: **Checked** ✓ (allows playing while alt-tabbed)
- Resizable Window: **Checked** ✓ (user can resize)
- Visible In Background: **Checked** ✓

#### **Splash Screen**
- Show Unity Logo: **Unchecked** ☐ (if using Unity Personal/Plus)
  - Note: Pro license required to remove Unity splash
- For Personal license: Accept default Unity splash

#### **Other Settings**
- Color Space: **Linear** (better visual quality)
- Auto Graphics API: **Checked** ✓
- Scripting Backend: **IL2CPP** (smaller, faster) OR **Mono** (faster builds)
  - Recommended: **IL2CPP** for release builds
  - Use **Mono** for quick test builds

#### **Optimization**
- API Compatibility Level: **.NET Standard 2.1**
- Managed Stripping Level: **Medium** (balances size and compatibility)
- Strip Engine Code: **Checked** ✓ (reduces build size)
- Vertex Compression: **Mixed** (default is fine)

---

## Build Process

### Step 1: Pre-Build Checklist

- [ ] All scenes added to Build Settings (MainScene.unity)
- [ ] No console errors in Unity Editor
- [ ] Test in Editor one final time (Play Mode)
- [ ] Verify all Inspector references assigned
- [ ] Check that InputActions C# class is generated
- [ ] Ensure audio clips are assigned in AudioManager

### Step 2: Build the Executable

1. **Create Build Folder**
   ```
   Project Root
   └── StandaloneBuilds/
       └── Windows/
           └── MushroomGenerator_v1.0.0/
   ```

2. **Configure Build Settings**
   - File → Build Settings
   - Scene list should show: `Scenes/MainScene`
   - Target Platform: Windows x86_64
   - Click **Build**

3. **Select Output Folder**
   - Navigate to `StandaloneBuilds/Windows/MushroomGenerator_v1.0.0/`
   - Unity will create: `MushroomGenerator.exe` + data folder

4. **Wait for Build**
   - IL2CPP builds take 5-10 minutes (first time longer)
   - Mono builds take 1-2 minutes
   - Progress shown in Unity Editor bottom bar

### Step 3: Post-Build Verification

1. **Check Build Output**
   ```
   MushroomGenerator_v1.0.0/
   ├── MushroomGenerator.exe          (executable)
   ├── MushroomGenerator_Data/        (assets and data)
   │   ├── Managed/                   (game code)
   │   ├── Plugins/                   (native plugins)
   │   ├── Resources/                 (resources)
   │   ├── StreamingAssets/           (streaming assets)
   │   ├── level0                     (scene data)
   │   ├── globalgamemanagers         (global settings)
   │   ├── globalgamemanagers.assets
   │   ├── resources.assets
   │   └── sharedassets0.assets
   ├── MonoBleedingEdge/              (Mono runtime, if using Mono backend)
   ├── UnityCrashHandler64.exe        (crash reporting)
   └── UnityPlayer.dll                (Unity engine)
   ```

2. **Test the Build**
   - Double-click `MushroomGenerator.exe`
   - Verify all features work:
     - WASD movement (+ Shift sprint)
     - Mouse hover highlighting
     - Mouse click selection
     - Coordinate search (magnifying glass)
     - Audio playback
     - Window resizing
   - Test at different window sizes
   - Close cleanly (no hanging processes)

3. **Check File Size**
   - Expected size: 30-80 MB depending on backend
   - IL2CPP: ~50-80 MB (larger but faster)
   - Mono: ~30-50 MB (smaller but slightly slower)

---

## Packaging for Distribution

### Step 1: Create README for End Users

Create `README.txt` in the build folder:

```
Mushroom Generator v1.0.0
=========================

A procedural mushroom generator based on infinite seed-based generation.
Explore an endless field of randomly generated mushrooms!

CONTROLS:
---------
WASD         - Move camera
Shift + WASD - Move camera faster
Mouse        - Hover over mushrooms to highlight
Left Click   - Select mushroom (plays sound)
Magnifying Glass Icon - Open coordinate search
Enter        - Navigate to coordinates (when search open)
Tab          - Toggle between X and Y fields

REQUIREMENTS:
------------
- Windows 7 64-bit or later
- DirectX 11 or later
- 2 GB RAM minimum
- 100 MB disk space

TROUBLESHOOTING:
---------------
- If exe doesn't run: Install Visual C++ Redistributable 2015-2022
  https://aka.ms/vs/17/release/vc_redist.x64.exe

- If no audio: Check Windows audio mixer

- If performance issues: Try lowering screen resolution

CREDITS:
-------
Migrated from C++ olcPixelGameEngine implementation
Built with Unity 6000.3.5f2

For source code and issues:
https://github.com/[your-username]/mushroom-generator
```

### Step 2: Compress for Upload

1. **Select All Build Files**
   - `MushroomGenerator.exe`
   - `MushroomGenerator_Data/` (entire folder)
   - `MonoBleedingEdge/` (if Mono backend)
   - `UnityCrashHandler64.exe`
   - `UnityPlayer.dll`
   - `README.txt`

2. **Compress to ZIP**
   - Right-click → Send to → Compressed (zipped) folder
   - Name: `MushroomGenerator_v1.0.0_Windows.zip`

3. **Verify ZIP**
   - Extract to temp location
   - Run exe from extracted folder
   - Confirm it works

---

## GitHub Release Process

### Step 1: Create Release on GitHub

1. **Navigate to Repository**
   - Go to: https://github.com/[your-username]/mushroom-generator

2. **Create New Release**
   - Click "Releases" (right sidebar)
   - Click "Draft a new release"

3. **Release Configuration**
   - Tag version: `v1.0.0`
   - Release title: `Mushroom Generator v1.0.0 - Initial Release`
   - Description (template below)

### Step 2: Release Description Template

```markdown
# Mushroom Generator v1.0.0

First stable release of the Unity migration from C++ olcPixelGameEngine.

## ✨ Features

- **Infinite Procedural Generation**: Explore an endless field of mushrooms
- **Three Mushroom Types**: Bolete (red), Roundhead (green), Chanterelle (yellow)
- **Smooth Camera Controls**: WASD movement with sprint (Shift)
- **Mouse Interaction**: Hover highlighting and click selection
- **Coordinate Search**: Jump to specific coordinates
- **Real-time Coordinate Tracker**: See current position in bottom-left corner
- **Audio Feedback**: Sound effects for interactions

## 🎮 Controls

| Input | Action |
|-------|--------|
| WASD | Move camera |
| Shift + WASD | Sprint (2× speed) |
| Mouse Hover | Highlight mushroom |
| Left Click | Select mushroom |
| Magnifying Glass | Open coordinate search |
| Enter | Navigate to coordinates |
| Tab | Toggle between X/Y fields |

## 📥 Download

Download the Windows 64-bit build below:
- **[MushroomGenerator_v1.0.0_Windows.zip](link-will-be-auto-generated)**

Extract the ZIP and run `MushroomGenerator.exe`

## 🛠️ System Requirements

- **OS**: Windows 7 64-bit or later
- **Graphics**: DirectX 11 compatible
- **Memory**: 2 GB RAM
- **Storage**: 100 MB available space

## 🐛 Known Issues

- None currently

## 📝 Changelog

Initial release - migrated from C++ with the following improvements:
- Flexible window resizing
- Modern input system with keyboard remapping support
- Coordinate search functionality
- Real-time coordinate tracking
- Audio system integration

## 🔗 Links

- [Source Code](https://github.com/[your-username]/mushroom-generator)
- [Report Issue](https://github.com/[your-username]/mushroom-generator/issues)
```

### Step 3: Upload Build

1. **Attach Binary**
   - Drag `MushroomGenerator_v1.0.0_Windows.zip` to release assets area
   - Wait for upload to complete

2. **Publish Release**
   - Check "Set as the latest release" ✓
   - Click "Publish release"

---

## Testing Across Devices

### Local Testing
1. Copy build folder to different PC
2. Run without Unity installed
3. Verify all features work
4. Test on different Windows versions if possible

### Distribution Testing
1. Download your own release ZIP from GitHub
2. Extract to clean folder (no Unity)
3. Run and verify

### Performance Testing
- Test on lower-end hardware if available
- Check frame rate (should maintain 60 FPS)
- Monitor memory usage (Task Manager)
- Verify startup time (< 10 seconds)

---

## Build Size Optimization

### Reducing Build Size

1. **Strip Unused Code**
   - Player Settings → Other Settings
   - Managed Stripping Level: **High**
   - Enable: "Strip Engine Code"

2. **Optimize Audio**
   - Audio clips: Use compressed formats (MP3 or Ogg Vorbis)
   - Sample rate: 22050 Hz for SFX (44100 Hz not needed)

3. **Optimize Textures**
   - Mushroom sprites: Use compressed formats (DXT5 for PC)
   - Max size: 2048×2048 (current sprites are 46×53, very small)

4. **Remove Unnecessary Packages**
   - Check Package Manager
   - Remove: Visual Scripting (if not using)
   - Keep: Input System, URP, 2D tools

5. **IL2CPP Backend**
   - Smaller final size than Mono
   - Better performance
   - Longer build times (acceptable for releases)

### Expected Size Breakdown
```
Total: ~50 MB (IL2CPP) / ~35 MB (Mono)
├── Executable: ~2 MB
├── UnityPlayer.dll: ~25 MB
├── Managed code: ~5-15 MB
├── Engine code: ~10-20 MB
└── Assets (sprites, audio): ~5 MB
```

---

## Automation (Optional)

### Unity Cloud Build
- Configure automatic builds on commit
- Sign up: https://unity.com/products/cloud-build
- Connect GitHub repository
- Set build configuration
- Builds trigger on git push

### Local Build Script
Create `BuildScript.cs` in `Assets/Editor/`:

```csharp
using UnityEditor;
using UnityEditor.Build.Reporting;

public class BuildScript
{
    [MenuItem("Build/Build Windows 64-bit")]
    public static void BuildWindows64()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/MainScene.unity" };
        buildPlayerOptions.locationPathName = "StandaloneBuilds/Windows/MushroomGenerator.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            UnityEngine.Debug.Log("Build failed");
        }
    }
}
```

Then use: Build → Build Windows 64-bit

---

## WebGL Build (Alternative)

For browser-based testing:

### Build Settings
- Platform: **WebGL**
- Compression Format: **Gzip** (best compatibility)
- Enable Exceptions: **Explicitly Thrown Exceptions Only**

### Hosting Options
1. **itch.io** (easiest): https://itch.io
2. **GitHub Pages** (free): Enable in repo settings
3. **Netlify** (free tier): https://netlify.com

### WebGL Limitations
- No local file access
- Slower performance than standalone
- Larger download size (~60-100 MB)
- Requires modern browser

**Recommendation**: Use standalone Windows build for best experience.

---

## Post-Release Monitoring

### Metrics to Track
- Download count (GitHub Insights)
- Issue reports (GitHub Issues)
- User feedback (comments, emails)

### Update Cycle
1. Fix critical bugs immediately (v1.0.1)
2. Minor updates as needed (v1.1.0)
3. Major features in new versions (v2.0.0)

---

## Version Numbering

**Format**: `v<major>.<minor>.<patch>`

- **Major** (v2.0.0): Breaking changes, major new features
- **Minor** (v1.1.0): New features, backwards compatible
- **Patch** (v1.0.1): Bug fixes, no new features

**Examples**:
- v1.0.0 - Initial release
- v1.0.1 - Fix audio bug
- v1.1.0 - Add field guide UI
- v2.0.0 - Add 3D mode (breaking change)

---

## Rollback Plan

If release has critical issues:

1. **Mark Release as Pre-release**
   - Edit release on GitHub
   - Check "Set as a pre-release" ✓
   - Update title: "v1.0.0 (Known Issues)"

2. **Pin Working Version**
   - Previous working version becomes "Latest"
   - New users download stable version

3. **Fix and Re-release**
   - Fix issues
   - Build v1.0.1
   - Mark as "Latest release"

---

## Support and Maintenance

### User Support Channels
- GitHub Issues: For bug reports
- GitHub Discussions: For questions/feedback
- Email: (optional) for direct contact

### Issue Template
Create `.github/ISSUE_TEMPLATE/bug_report.md`:

```markdown
---
name: Bug Report
about: Report a bug or issue
---

**Describe the bug**
A clear description of what the bug is.

**To Reproduce**
Steps to reproduce:
1. Go to '...'
2. Click on '...'
3. See error

**Expected behavior**
What you expected to happen.

**Screenshots**
If applicable, add screenshots.

**System Info**
- OS: [e.g. Windows 10]
- Version: [e.g. v1.0.0]

**Additional context**
Any other information about the problem.
```

---

## Next Steps

After v1.0.0 release:

1. **Monitor Feedback** (first week)
   - Check for critical bugs
   - Respond to issues quickly

2. **Plan v1.1.0** (after stable period)
   - Add info window (Phase 4 from migration plan)
   - Add field guide UI
   - Consider save system

3. **Long-term Goals**
   - WebGL version for browser play
   - Mobile port (touch controls)
   - Steam release (if scope expands)

---

## Quick Reference Commands

**Build from Unity Menu**:
```
File → Build Settings → Build
```

**Build from Command Line** (automated):
```bash
"C:\Program Files\Unity\Hub\Editor\6000.3.5f2\Editor\Unity.exe" ^
  -quit -batchmode -projectPath "C:\Repos\mushroom-generator" ^
  -buildWindows64Player "StandaloneBuilds/Windows/MushroomGenerator.exe"
```

**Create ZIP** (PowerShell):
```powershell
Compress-Archive -Path "StandaloneBuilds\Windows\MushroomGenerator_v1.0.0\*" `
  -DestinationPath "MushroomGenerator_v1.0.0_Windows.zip"
```

---

## Conclusion

This deployment plan provides a complete workflow for building, packaging, and distributing the Mushroom Generator as a lightweight Windows executable. The GitHub Releases approach ensures easy distribution and version management.

For questions or improvements to this plan, open an issue on GitHub.
