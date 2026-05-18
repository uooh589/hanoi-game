#!/bin/bash
# Publish a new release — bump version, build, package, and optionally push to GitHub.
# Usage: ./publish.sh <version> [--github <user/repo>] [--token <gh_token>]
set -e

UNITY="/home/liuhan/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity"
PROJECT="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$PROJECT/Builds"
VERSION="${1:?Usage: ./publish.sh <version> [--github user/repo]}"
GITHUB_REPO=""
GH_TOKEN=""

shift
while [[ $# -gt 0 ]]; do
    case "$1" in
        --github) GITHUB_REPO="$2"; shift 2 ;;
        --token)   GH_TOKEN="$2"; shift 2 ;;
        *) echo "Unknown: $1"; exit 1 ;;
    esac
done

echo "========================================="
echo " Publishing Hanoi Game $VERSION"
echo "========================================="

# 1. Update version everywhere
echo "$VERSION" > "$PROJECT/VERSION"
sed -i "s/CurrentVersion = \".*\"/CurrentVersion = \"$VERSION\"/" \
    "$PROJECT/Assets/Scripts/Utilities/VersionManager.cs"
echo "[1/5] Version -> $VERSION"

# 2. Clean + rebuild scene
pkill -9 Unity 2>/dev/null || true; sleep 2
rm -rf "$PROJECT/Library/ScriptAssemblies" "$PROJECT/Library/Bee" "$PROJECT/Temp"
echo "[2/5] Caches cleaned"

$UNITY -quit -batchmode -nographics -projectPath "$PROJECT" \
    -executeMethod HanoiGame.SceneBuilder.Build -logFile /tmp/pub_scene.log 2>&1
echo "[3/5] Scene rebuilt"

# 3. Build
rm -rf "$BUILD_DIR/Windows"/*
$UNITY -quit -batchmode -nographics -projectPath "$PROJECT" \
    -buildTarget Win64 -buildWindows64Player "$BUILD_DIR/Windows/HanoiGame.exe" \
    -logFile /tmp/pub_win.log 2>&1

rm -rf "$BUILD_DIR/Linux"/*
$UNITY -quit -batchmode -nographics -projectPath "$PROJECT" \
    -buildTarget Linux64 -buildLinux64Player "$BUILD_DIR/Linux/HanoiGame" \
    -logFile /tmp/pub_linux.log 2>&1

cp "$PROJECT/Builds/HanoiGame_Windows/游戏教程.txt" "$BUILD_DIR/Windows/" 2>/dev/null || true
echo "[4/5] Builds complete"

# 4. Package
cd "$BUILD_DIR"
zip -rq "HanoiGame_${VERSION}_Win64.zip" Windows/
tar -czf "HanoiGame_${VERSION}_Linux.tar.gz" Linux/
echo "[5/5] Packages:"
ls -lh "HanoiGame_${VERSION}_"*

# 5. GitHub release
if [ -n "$GITHUB_REPO" ] && [ -n "$GH_TOKEN" ]; then
    echo ""
    echo "=== Creating GitHub Release ==="
    gh auth login --with-token <<< "$GH_TOKEN" 2>/dev/null || true

    # Create release
    gh release create "$VERSION" \
        --repo "$GITHUB_REPO" \
        --title "Hanoi Game $VERSION" \
        --notes "Release $VERSION" \
        "HanoiGame_${VERSION}_Win64.zip" \
        "HanoiGame_${VERSION}_Linux.tar.gz" 2>&1 || echo "gh failed — push manually"

    echo "Update check URL: https://raw.githubusercontent.com/${GITHUB_REPO}/main/VERSION"
elif [ -n "$GITHUB_REPO" ]; then
    echo ""
    echo "To create GitHub release, run:"
    echo "  gh release create $VERSION --repo $GITHUB_REPO \\"
    echo "    --title \"Hanoi Game $VERSION\" \\"
    echo "    Builds/HanoiGame_${VERSION}_Win64.zip \\"
    echo "    Builds/HanoiGame_${VERSION}_Linux.tar.gz"
fi

echo ""
echo "Done! Packages in: Builds/"
