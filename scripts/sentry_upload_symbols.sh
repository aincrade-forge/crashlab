#!/usr/bin/env bash
set -euo pipefail

# Sentry symbol upload helper (dSYM, PDB, IL2CPP line maps / NDK)
# Requires sentry-cli in PATH and SENTRY_AUTH_TOKEN, SENTRY_ORG, SENTRY_PROJECT env vars.
# Usage examples:
#  PLATFORM=ios DSYM_DIR=path/to/dSYMs ./scripts/sentry_upload_symbols.sh
#  PLATFORM=android ANDROID_LIB_DIR=path/to/lib ANDROID_LINEMAP_DIR=path/to/LineMaps ./scripts/sentry_upload_symbols.sh
#  PLATFORM=windows PDB_DIR=path/to/pdbs ./scripts/sentry_upload_symbols.sh

: "${SENTRY_ORG:?Set SENTRY_ORG}"
: "${SENTRY_PROJECT:?Set SENTRY_PROJECT}"
: "${SENTRY_AUTH_TOKEN:?Set SENTRY_AUTH_TOKEN}"

case "${PLATFORM:-}" in
  ios|macos)
    : "${DSYM_DIR:?Set DSYM_DIR to directory or glob of dSYM bundles}"
    echo "[Sentry] Uploading dSYM(s) from $DSYM_DIR"
    sentry-cli upload-dif --include-sources "$DSYM_DIR"
    ;;
  android)
    : "${ANDROID_LIB_DIR:?Set ANDROID_LIB_DIR to directory with .so files}"
    echo "[Sentry] Uploading Android NDK symbols from $ANDROID_LIB_DIR"
    sentry-cli upload-dif "$ANDROID_LIB_DIR"
    if [[ -n "${ANDROID_LINEMAP_DIR:-}" ]]; then
      echo "[Sentry] Uploading IL2CPP line maps from $ANDROID_LINEMAP_DIR"
      sentry-cli upload-dif --il2cpp "$ANDROID_LINEMAP_DIR"
    fi
    ;;
  windows)
    : "${PDB_DIR:?Set PDB_DIR to directory with PDBs}"
    echo "[Sentry] Uploading PDBs from $PDB_DIR"
    sentry-cli upload-dif "$PDB_DIR"
    ;;
  *)
    echo "Set PLATFORM to one of: ios, macos, android, windows"; exit 2;
    ;;
esac

echo "Done."

