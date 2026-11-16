#!/usr/bin/env bash

set -euo pipefail

runtime="${1:?runtime identifier is required}"
version="${2:?version is required}"
output_directory="${3:?output directory is required}"
project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
publish_directory="$(mktemp -d)"
bundle_directory="$(mktemp -d)/SumScription.app"

cleanup() {
  rm -rf "$publish_directory" "$(dirname "$bundle_directory")"
}
trap cleanup EXIT

dotnet publish "$project_root/src/SumScription.App/SumScription.App.csproj" \
  --configuration Release \
  --runtime "$runtime" \
  --self-contained true \
  --output "$publish_directory" \
  -p:Version="$version" \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

mkdir -p "$bundle_directory/Contents/MacOS" "$bundle_directory/Contents/Resources"
cp -R "$publish_directory"/. "$bundle_directory/Contents/MacOS/"
rm -rf "$bundle_directory/Contents/MacOS/Assets"
cp "$project_root/src/SumScription.App/Assets/app-icon.png" \
  "$bundle_directory/Contents/Resources/app-icon.png"
cp "$project_root/src/SumScription.App/Assets/app-icon.icns" \
  "$bundle_directory/Contents/Resources/app-icon.icns"
chmod +x "$bundle_directory/Contents/MacOS/SumScription.App"

cat > "$bundle_directory/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleDisplayName</key>
  <string>SumScription</string>
  <key>CFBundleExecutable</key>
  <string>SumScription.App</string>
  <key>CFBundleIconFile</key>
  <string>app-icon</string>
  <key>CFBundleIdentifier</key>
  <string>com.reathloo.sumscription</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundleName</key>
  <string>SumScription</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>$version</string>
  <key>CFBundleVersion</key>
  <string>$version</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

/usr/bin/xattr -cr "$bundle_directory"
/usr/bin/codesign --force --sign - --timestamp=none \
  "$bundle_directory/Contents/MacOS/SumScription.App"
/usr/bin/codesign --force --sign - --timestamp=none "$bundle_directory"
/usr/bin/codesign --verify --deep --strict "$bundle_directory"

mkdir -p "$output_directory"
archive="$output_directory/SumScription-macOS-${runtime#osx-}-v$version.zip"
/usr/bin/ditto -c -k --norsrc --keepParent "$bundle_directory" "$archive"
echo "$archive"
