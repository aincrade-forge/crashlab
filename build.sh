#!/usr/bin/env bash
set -euo pipefail

# Usage: TARGET=macos-arm64 FLAVOR=unity DEV_MODE=false ./build.sh

PROJECT_PATH="$(cd "$(dirname "$0")" && pwd)"
UNITY_VERSION=$(sed -n 's/^m_EditorVersion: \(.*\)$/\1/p' "$PROJECT_PATH/ProjectSettings/ProjectVersion.txt")
UNITY_APP="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"

if [[ ! -x "$UNITY_APP" ]]; then
  echo "Unity not found at $UNITY_APP"
  echo "Edit build.sh UNITY_APP path or install the required version."
  exit 2
fi

: "${TARGET:=macos-arm64}"
: "${FLAVOR:=unity}"
: "${DEV_MODE:=false}"

LOG_PATH="$PROJECT_PATH/build-${TARGET}-${FLAVOR}.log"

echo "Building target=$TARGET flavor=$FLAVOR dev_mode=$DEV_MODE"

TARGET="$TARGET" FLAVOR="$FLAVOR" DEV_MODE="$DEV_MODE" \
"$UNITY_APP" -batchmode -quit -nographics \
  -projectPath "$PROJECT_PATH" \
  -logFile "$LOG_PATH" \
  -executeMethod BuildScripts.BuildRelease

echo "Build finished. See log: $LOG_PATH"
