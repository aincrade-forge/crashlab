# CrashLab

Unity project for testing and comparing crash reporting across Sentry, Firebase Crashlytics, and Unity Diagnostics on iOS, Android, macOS, and Windows (all IL2CPP).

## Quick Start
- Unity version: 6000.1.10f1
- Open with Unity Hub → Add this folder → Open.
- Scene: `Assets/Scenes/SampleScene.unity` (placeholder).

## Build
- Editor: File → Build Settings → select target → Switch Platform → Build.
- Player settings: use IL2CPP backend and ARM64/x86_64 as applicable.
- Artifacts and symbols to keep:
  - Android: `.apk`/`.aab`, IL2CPP line maps, `.so`
  - iOS/macOS: `.ipa`/`.app`, dSYMs
  - Windows: `.exe`, PDBs

CLI helpers:
- Single build: `TARGET=windows-x64 FLAVOR=sentry DEV_MODE=false ./build.sh`
- Build matrix: `MATRIX=true TARGETS="windows-x64,macos-arm64,android-arm64,ios-arm64" ./build.sh`
- Test matrix (EditMode): `TESTS=true FLAVORS="sentry,unity" ./build.sh`
  - See `docs/build-matrix.md` for supported flavor/platform combos.

Bundle IDs / Package Names
- Control identifiers via env vars when building:
  - `BUNDLE_ID` (applies to all platforms)
  - `BUNDLE_ID_ANDROID` or `ANDROID_APPLICATION_ID`
  - `BUNDLE_ID_IOS` or `IOS_BUNDLE_ID`
- Defaults if unset: `com.aincrade.crashlab.<flavor>.<platform>`

## Symbols Upload
- Default behavior: after each build, the Editor post-build hook auto-attempts symbol upload.
  - Crashlytics (Android): uploads if `Assets/google-services.json` exists and symbols are found.
  - Sentry (macOS/Android): uploads if `SENTRY_ORG`, `SENTRY_PROJECT`, `SENTRY_AUTH_TOKEN` are set.
  - iOS: upload after Xcode archive (use scripts in `scripts/`).
- Opt out by setting `CRASHLAB_NO_UPLOAD_SYMBOLS=true` before building.

## Repo Layout
- `Assets/` — game content and code (add scripts under `Assets/Scripts/`).
- `Packages/` — Unity packages (`manifest.json`).
- `ProjectSettings/` — project/editor settings.
- `.gitignore` — tuned for Unity (excludes `Library/`, `Logs/`, `Temp/`, builds, IDE files).

## Crash Lab Goal
- Produce apples-to-apples comparisons between backends and platforms:
  - One backend active per build (Sentry, Crashlytics, or Unity Diagnostics).
  - Standard set of crash/exception triggers.
  - Symbols uploaded for accurate stack traces.
- Details and acceptance criteria: see `requirements.md`.

## Telemetry & Metadata
- The app sets common metadata at startup (see `Assets/Scripts/CrashLabTelemetry.cs`).
- Provide env vars to stamp events: `RUN_ID`, `RELEASE_NAME`, `ENVIRONMENT`, `COMMIT_SHA`, `BUILD_NUMBER`, `DEV_MODE`, `USER_ID`, `CI`, `SERVER_NAME`.
- Select telemetry provider via defines: `DIAG_SENTRY`, `DIAG_CRASHLYTICS`, or `DIAG_UNITY`.
- Editor menu: CrashLab → Telemetry → Use Sentry | Use Crashlytics | Use Unity Diagnostics | Use None
  - Status: CrashLab → Telemetry → Status shows per‑platform defines and active provider.
- Unity Diagnostics (Cloud Diagnostics) initializes under `DIAG_UNITY` using UGS (`com.unity.services.core`, `com.unity.services.cloud-diagnostics`).
  - Link the project to UGS in Project Settings → Services, and set the desired environment.
  - The app sets `userId` and custom metadata (run_id, commit_sha, build_number, dev_mode, backend, platform, server_name).

## Sentry Setup
- Sentry Unity SDK is added via UPM using a Git URL (see `docs/SENTRY_SETUP.md`).
- Provide `SENTRY_DSN` via environment. This project initializes Sentry in code and supports additional configuration via env vars (traces sample rate, PII, debug, in‑app include/exclude).

## Crashlytics via Tarballs + Git LFS
- We vendor Firebase tarballs in `Packages/vendor/` with versioned filenames and track them in Git LFS.
- See `docs/CRASHLYTICS_SETUP.md` for steps to add/replace tarballs and credentials files.
- Ensure `google-services.json` (Android) and `GoogleService-Info.plist` (iOS) are present under `Assets/`.

## Contributing
- Read `AGENTS.md` for repository conventions (structure, style, tests, PRs).
- Use Conventional Commits (e.g., `feat(player): add dash`).

## UI & Triggers
- In‑scene UI: use `CrashLab.UI.CrashUIBuilder` on your layout container and assign a `CrashUIButton` prefab to populate buttons at runtime.
- Headless triggers: `CrashHeadlessTriggers` handles deep links and Android intent extras; it also runs any scheduled startup crash.

## Notes
- Build/test matrix and telemetry switching: `docs/build-matrix.md`.
- Crash/error coverage expectations per platform/provider: `docs/error-crash-matrix.md`.
