#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

if ! command -v clang++ >/dev/null 2>&1; then
  echo "clang++ not found. Install Xcode Command Line Tools: xcode-select --install" >&2
  exit 1
fi

BUNDLE="build/PetMac.bundle"
rm -rf "$BUNDLE"
mkdir -p "$BUNDLE/Contents/MacOS"

cat > "$BUNDLE/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>PetMac</string>
    <key>CFBundleIdentifier</key>
    <string>com.fumi.xiaosu.petmac</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>PetMac</string>
    <key>CFBundlePackageType</key>
    <string>BNDL</string>
    <key>CFBundleShortVersionString</key>
    <string>0.1.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
</dict>
</plist>
PLIST

clang++ -std=c++17 -dynamiclib -fobjc-arc \
  -framework Cocoa \
  -framework CoreGraphics \
  -framework ApplicationServices \
  -o "$BUNDLE/Contents/MacOS/PetMac" \
  PetMac.mm

mkdir -p ../../Assets/Plugins/macOS
rm -rf ../../Assets/Plugins/macOS/PetMac.bundle
cp -R "$BUNDLE" ../../Assets/Plugins/macOS/

echo "Built $BUNDLE"
echo "Copied to Assets/Plugins/macOS/PetMac.bundle"
echo "Open the project in Unity on macOS and build the .app; this script does not test it."
