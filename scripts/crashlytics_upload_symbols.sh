#!/usr/bin/env bash
set -euo pipefail

# Crashlytics symbol upload helper
# Usage:
#  ANDROID=true \
#  GOOGLE_SERVICES_JSON=Assets/google-services.json \
#  ANDROID_SYMBOLS_DIR=Library/PlayerDataCache/Android/ \
#  ./scripts/crashlytics_upload_symbols.sh
#
#  IOS=true \
#  GOOGLE_SERVICE_INFO_PLIST=Assets/GoogleService-Info.plist \
#  IOS_DSYM_DIR=Builds/iOS/DerivedData/Build/Products/*.dSYM \
#  ./scripts/crashlytics_upload_symbols.sh

if [[ "${ANDROID:-false}" == "true" ]]; then
  : "${GOOGLE_SERVICES_JSON:?Set GOOGLE_SERVICES_JSON path}"
  : "${ANDROID_SYMBOLS_DIR:?Set ANDROID_SYMBOLS_DIR path to NDK/IL2CPP symbols}"
  if [[ ! -f "$GOOGLE_SERVICES_JSON" ]]; then echo "Missing $GOOGLE_SERVICES_JSON"; exit 2; fi
  if [[ ! -d "$ANDROID_SYMBOLS_DIR" ]]; then echo "Missing $ANDROID_SYMBOLS_DIR"; exit 2; fi
  echo "[Crashlytics] Uploading Android symbols from $ANDROID_SYMBOLS_DIR"
  ./Assets/Plugins/Android/Firebase/crashlytics/Tools/upload-symbols \
    -gsp "$GOOGLE_SERVICES_JSON" -p android "$ANDROID_SYMBOLS_DIR"
fi

if [[ "${IOS:-false}" == "true" ]]; then
  : "${GOOGLE_SERVICE_INFO_PLIST:?Set GOOGLE_SERVICE_INFO_PLIST path}"
  : "${IOS_DSYM_DIR:?Set IOS_DSYM_DIR glob or directory containing dSYM bundles}"
  if [[ ! -f "$GOOGLE_SERVICE_INFO_PLIST" ]]; then echo "Missing $GOOGLE_SERVICE_INFO_PLIST"; exit 2; fi
  echo "[Crashlytics] Uploading iOS dSYM(s) from $IOS_DSYM_DIR"
  ./Pods/FirebaseCrashlytics/upload-symbols -gsp "$GOOGLE_SERVICE_INFO_PLIST" -p ios $IOS_DSYM_DIR
fi

echo "Done."

