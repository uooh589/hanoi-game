---
name: publish-release
description: Build and publish a new release of Hanoi Game. Runs SceneBuilder, builds Win+Linux, packages, and creates GitHub Release.
disable-model-invocation: true
---

# Publish Hanoi Game Release

You are publishing a new version of Hanoi Game. This skill wraps the full publish workflow.

## Arguments

The user should provide a version number, e.g. `/publish v1.0.2`.

## Workflow

1. **Confirm version** — Ask the user to confirm the version number if not explicitly stated.

2. **Update version** — Update `VERSION` file and `VersionManager.cs`:
   ```bash
   echo "VERSION" > VERSION
   sed -i 's/CurrentVersion = ".*"/CurrentVersion = "VERSION"/' Assets/Scripts/Utilities/VersionManager.cs
   ```

3. **Rebuild scene** (CRITICAL — always do this first):
   ```bash
   pkill -9 Unity 2>/dev/null; sleep 2
   rm -rf Library/ScriptAssemblies Library/Bee Temp
   /home/liuhan/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity -quit -batchmode -nographics \
     -projectPath /home/liuhan/hanoi-game \
     -executeMethod HanoiGame.SceneBuilder.Build \
     -logFile /tmp/pub_scene.log
   ```

4. **Build Windows + Linux**:
   ```bash
   rm -rf Builds/Windows/* Builds/Linux/*
   /home/liuhan/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity -quit -batchmode -nographics \
     -projectPath /home/liuhan/hanoi-game \
     -buildTarget Win64 -buildWindows64Player Builds/Windows/HanoiGame.exe \
     -logFile /tmp/pub_win.log
   /home/liuhan/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity -quit -batchmode -nographics \
     -projectPath /home/liuhan/hanoi-game \
     -buildTarget Linux64 -buildLinux64Player Builds/Linux/HanoiGame \
     -logFile /tmp/pub_linux.log
   ```

5. **Package**:
   ```bash
   cd Builds
   cp ../Builds/HanoiGame_Windows/游戏教程.txt Windows/ 2>/dev/null || true
   zip -rq "HanoiGame_VERSION_Win64.zip" Windows/
   tar -czf "HanoiGame_VERSION_Linux.tar.gz" Linux/
   ```

6. **Git commit + push**:
   ```bash
   git add VERSION Assets/Scripts/Utilities/VersionManager.cs
   git commit -m "release: VERSION"
   git push origin master
   ```

7. **GitHub Release**:
   ```bash
   gh release create "VERSION" \
     --repo uooh589/hanoi-game \
     --title "Hanoi Game VERSION" \
     --notes "Release VERSION" \
     "Builds/HanoiGame_VERSION_Win64.zip" \
     "Builds/HanoiGame_VERSION_Linux.tar.gz"
   ```

8. **Report** — Show version, commit hash, and download URLs.
