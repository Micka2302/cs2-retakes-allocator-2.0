#!/usr/bin/env bash

TARGET_NAME="RetakesAllocator"
TARGET_DIR="./bin/Release/net8.0"
NEW_DIR="./bin/Release/RetakesAllocator"
SHARED_OUT="$NEW_DIR/shared/KitsuneMenu"
SHARED_SRC_GAME="./game/csgo/addons/counterstrikesharp/shared/KitsuneMenu/KitsuneMenu.dll"
SHARED_SRC_LOCAL="./KitsuneMenu/src/bin/Release/net8.0/KitsuneMenu.dll"

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

echo "Preparing shared/KitsuneMenu"
mkdir -p "$SHARED_OUT"
if [ -f "$SHARED_SRC_GAME" ]; then
  echo "Copying KitsuneMenu from shared path: $SHARED_SRC_GAME"
  cp "$SHARED_SRC_GAME" "$SHARED_OUT/"
elif [ -f "$SHARED_SRC_LOCAL" ]; then
  echo "Copying KitsuneMenu from local build: $SHARED_SRC_LOCAL"
  cp "$SHARED_SRC_LOCAL" "$SHARED_OUT/"
else
  echo "WARNING: KitsuneMenu.dll not found (checked $SHARED_SRC_GAME and $SHARED_SRC_LOCAL)"
fi

tree ./bin
