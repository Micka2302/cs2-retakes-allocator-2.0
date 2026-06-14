#!/usr/bin/env bash

TARGET_NAME="RetakesAllocator"
TARGET_DIR="./bin/Release/net8.0"
NEW_DIR="./bin/Release/RetakesAllocator"
SHARPMODMENU_ROOT="${SHARPMODMENU_ROOT:-/c/Users/micka/Documents/GitHub/SharpModMenu}"
SHARPMODMENU_COMPILED="$SHARPMODMENU_ROOT/compiled/counterstrikesharp"

echo $TARGET_NAME
echo $TARGET_DIR
echo $NEW_DIR

ls $TARGET_DIR/**

echo cp -r $TARGET_DIR $NEW_DIR
cp -r $TARGET_DIR $NEW_DIR
echo rm -rf "$NEW_DIR/runtimes"
rm -rf "$NEW_DIR/runtimes"
echo mkdir "$NEW_DIR/runtimes"
mkdir "$NEW_DIR/runtimes"
echo cp -rf "$TARGET_DIR/runtimes/linux-x64" "$NEW_DIR/runtimes"
cp -rf "$TARGET_DIR/runtimes/linux-x64" "$NEW_DIR/runtimes"
echo cp -rf "$TARGET_DIR/runtimes/win-x64" "$NEW_DIR/runtimes"
cp -rf "$TARGET_DIR/runtimes/win-x64" "$NEW_DIR/runtimes"

# Remove unnecessary files
rm "$NEW_DIR/CounterStrikeSharp.API.dll"

if [ -d "$SHARPMODMENU_COMPILED" ]; then
  echo "Copying SharpModMenu from: $SHARPMODMENU_COMPILED"
  mkdir -p "$NEW_DIR/plugins" "$NEW_DIR/shared" "$NEW_DIR/configs/plugins"
  [ -d "$SHARPMODMENU_COMPILED/plugins/SharpModMenu" ] && cp -rf "$SHARPMODMENU_COMPILED/plugins/SharpModMenu" "$NEW_DIR/plugins/"
  [ -d "$SHARPMODMENU_COMPILED/shared/SharpModMenu" ] && cp -rf "$SHARPMODMENU_COMPILED/shared/SharpModMenu" "$NEW_DIR/shared/"
  [ -d "$SHARPMODMENU_COMPILED/shared/CSSUniversalMenuAPI" ] && cp -rf "$SHARPMODMENU_COMPILED/shared/CSSUniversalMenuAPI" "$NEW_DIR/shared/"
  [ -d "$SHARPMODMENU_COMPILED/configs/plugins/SharpModMenu" ] && cp -rf "$SHARPMODMENU_COMPILED/configs/plugins/SharpModMenu" "$NEW_DIR/configs/plugins/"
  if [ -f "$NEW_DIR/configs/plugins/SharpModMenu/sharpmodmenu_config.jsonc" ]; then
    sed -i "/\"HtmlCompactFooterMessage\"/c\\\t\"HtmlCompactFooterMessage\": \"<font color='#D10D0D'>Select: </font><font color='#F2A10F'>ZS/Use</font>\"," "$NEW_DIR/configs/plugins/SharpModMenu/sharpmodmenu_config.jsonc"
  fi
else
  echo "WARNING: SharpModMenu compiled output not found at $SHARPMODMENU_COMPILED. Build SharpModMenu first or set SHARPMODMENU_ROOT."
fi

tree ./bin
