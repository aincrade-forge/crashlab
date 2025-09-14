#!/usr/bin/env bash
set -euo pipefail

# Usage:
#  - Single build: TARGET=macos-arm64 FLAVOR=unity DEV_MODE=false ./build.sh
#  - Build matrix: MATRIX=true TARGETS="windows-x64,macos-arm64,android-arm64,ios-arm64" ./build.sh
#  - Test matrix: TESTS=true FLAVORS="sentry,unity" ./build.sh

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
: "${MATRIX:=false}"
: "${TESTS:=false}"

LOG_PATH="$PROJECT_PATH/build-${TARGET}-${FLAVOR}.log"

if [[ "$MATRIX" == "true" ]]; then
  echo "Running build matrix targets=${TARGETS:-default} dev_mode=$DEV_MODE"
  TARGETS="${TARGETS:-windows-x64,macos-arm64,android-arm64,ios-arm64}" DEV_MODE="$DEV_MODE" \
  "$UNITY_APP" -batchmode -quit -nographics \
    -projectPath "$PROJECT_PATH" \
    -logFile "$LOG_PATH" \
    -executeMethod BuildScripts.BuildMatrix
  echo "Build matrix finished. See log: $LOG_PATH"
elif [[ "$TESTS" == "true" ]]; then
  echo "Running test matrix flavors=${FLAVORS:-auto}"
  FLAVORS="${FLAVORS:-}" \
  "$UNITY_APP" -batchmode -quit -nographics -runTests \
    -projectPath "$PROJECT_PATH" \
    -logFile "$LOG_PATH" \
    -executeMethod BuildScripts.TestMatrix \
    -testResults "$PROJECT_PATH/Logs/test-results.xml"
  echo "Test matrix finished. See log: $LOG_PATH"
else
  echo "Building target=$TARGET flavor=$FLAVOR dev_mode=$DEV_MODE"
  TARGET="$TARGET" FLAVOR="$FLAVOR" DEV_MODE="$DEV_MODE" \
  "$UNITY_APP" -batchmode -quit -nographics \
    -projectPath "$PROJECT_PATH" \
    -logFile "$LOG_PATH" \
    -executeMethod BuildScripts.BuildRelease
  echo "Build finished. See log: $LOG_PATH"
fi
