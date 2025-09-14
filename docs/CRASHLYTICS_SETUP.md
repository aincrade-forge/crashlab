# Crashlytics Setup (Tarballs)

This project integrates Firebase Crashlytics via Unity Package Manager tarballs from Google’s archive.

## 1) Download tarballs (versioned filenames)
From the official Firebase Unity SDK archive, download these packages (versions compatible with your Unity Editor) and keep the version in the filename:

- com.google.external-dependency-manager-<version>.tgz
- com.google.firebase.app-<version>.tgz
- com.google.firebase.crashlytics-<version>.tgz

Place them in: `Packages/vendor/` (keep the version in the filename). UPM is configured to reference versioned tarballs via `file:` in `Packages/manifest.json`.

## 2) Git LFS
- Install Git LFS once: `git lfs install`
- This repo tracks vendor tarballs via LFS (`.gitattributes`):
  - `Packages/vendor/*.tgz`
  - `Packages/vendor/*.tar.gz`
- After adding/replacing tarballs, commit normally and push; LFS will upload the large files.

## 3) Credentials files
- Android: place `google-services.json` under `Assets/` (or follow EDM4U prompts).
- iOS: place `GoogleService-Info.plist` under `Assets/`.

## 4) Open project
- Open Unity; Package Manager will install the tarballs.
- If prompted by EDM4U, resolve dependencies.

## 5) Build
- Use our build script: `TARGET=android-arm64 FLAVOR=crashlytics ./build.sh` or `TARGET=ios-arm64 FLAVOR=crashlytics ./build.sh`.

## Notes
- Crashlytics initialization happens in `CrashLabTelemetry` under `#if DIAG_CRASHLYTICS`.
- The app sets `userId` and custom keys (`run_id`, `commit_sha`, `build_number`, `dev_mode`, etc.) at startup.
- For IL2CPP symbolication, upload symbols per platform as described in `requirements.md`.
