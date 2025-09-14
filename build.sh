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

ARTIFACT_DIR="$PROJECT_PATH/Artifacts/${TARGET}-${FLAVOR}"
mkdir -p "$ARTIFACT_DIR"
LOG_PATH="$ARTIFACT_DIR/build.log"

if [[ "$MATRIX" == "true" ]]; then
  echo "Running build matrix targets=${TARGETS:-default} dev_mode=$DEV_MODE"
  IFS=',' read -r -a TARGET_ARR <<< "${TARGETS:-windows-x64,macos-arm64,android-arm64,ios-arm64}"
  flavors_for_target() {
    case "$1" in
      windows-x64) echo "sentry unity";;
      macos-arm64) echo "sentry unity";;
      android-arm64) echo "sentry crashlytics unity";;
      ios-arm64) echo "sentry crashlytics unity";;
      *) echo "";;
    esac
  }
  for t in "${TARGET_ARR[@]}"; do
    for f in $(flavors_for_target "$t"); do
      ARTIFACT_DIR="$PROJECT_PATH/Artifacts/${t}-${f}"
      mkdir -p "$ARTIFACT_DIR"
      LOG_PATH="$ARTIFACT_DIR/build.log"
      echo "Building $t / $f (dev=$DEV_MODE)" | tee "$LOG_PATH"
      TARGET="$t" FLAVOR="$f" DEV_MODE="$DEV_MODE" \
      "$UNITY_APP" -batchmode -quit -nographics \
        -projectPath "$PROJECT_PATH" \
        -logFile "$LOG_PATH" \
        -executeMethod BuildScripts.BuildRelease || exit 1
      echo "Built $t / $f → see $LOG_PATH"
    done
  done
  echo "Build matrix finished. Logs under Artifacts/<target>-<flavor>/build.log"
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
