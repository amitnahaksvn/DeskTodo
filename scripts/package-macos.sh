#!/usr/bin/env bash
# Builds a self-contained DeskTodo.app bundle and packages it into a .dmg.
#
# Usage: scripts/package-macos.sh [runtime-identifier]
#   runtime-identifier defaults to the host's own RID (osx-arm64 on Apple
#   Silicon, osx-x64 on Intel) via `dotnet --info`-style detection below.
#
# Not code-signed or notarized — that needs a real Apple Developer ID
# certificate this environment doesn't have. A user distributing this build
# outside their own machine would need to sign + notarize it themselves
# (`codesign`, `xcrun notarytool`) before Gatekeeper will open it without a
# right-click-Open workaround. See docs/ARCHITECTURE.md's "Phase 16" section.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
APP_CSPROJ="$REPO_ROOT/src/DeskTodo.App/DeskTodo.App.csproj"

RID="${1:-$(uname -m | grep -q arm64 && echo osx-arm64 || echo osx-x64)}"
VERSION="$(grep -m1 '<Version>' "$REPO_ROOT/Directory.Build.props" 2>/dev/null | sed -E 's/.*<Version>(.*)<\/Version>.*/\1/' || true)"
VERSION="${VERSION:-1.0.0}"

BUILD_DIR="$REPO_ROOT/artifacts/macos"
PUBLISH_DIR="$BUILD_DIR/publish-$RID"
APP_BUNDLE="$BUILD_DIR/DeskTodo.app"
DMG_PATH="$BUILD_DIR/DeskTodo-$VERSION-$RID.dmg"

echo "==> Publishing self-contained ($RID)…"
rm -rf "$PUBLISH_DIR"
dotnet publish "$APP_CSPROJ" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=false \
    -o "$PUBLISH_DIR"

echo "==> Assembling DeskTodo.app…"
rm -rf "$APP_BUNDLE"
mkdir -p "$APP_BUNDLE/Contents/MacOS" "$APP_BUNDLE/Contents/Resources"
cp -R "$PUBLISH_DIR/." "$APP_BUNDLE/Contents/MacOS/"
chmod +x "$APP_BUNDLE/Contents/MacOS/DeskTodo"

cat > "$APP_BUNDLE/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>DeskTodo</string>
    <key>CFBundleDisplayName</key>
    <string>DeskTodo</string>
    <key>CFBundleIdentifier</key>
    <string>com.desktodo.app</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleExecutable</key>
    <string>DeskTodo</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.productivity</string>
</dict>
</plist>
PLIST

echo "==> Creating DMG…"
rm -f "$DMG_PATH"
STAGING_DIR="$(mktemp -d)"
trap 'rm -rf "$STAGING_DIR"' EXIT
cp -R "$APP_BUNDLE" "$STAGING_DIR/"
ln -s /Applications "$STAGING_DIR/Applications"
hdiutil create -volname "DeskTodo $VERSION" -srcfolder "$STAGING_DIR" -ov -format UDZO "$DMG_PATH"

echo "==> Done: $DMG_PATH"
