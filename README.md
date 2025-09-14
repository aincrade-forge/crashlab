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

Example CLI (adjust path/version):
```
/Applications/Unity/Hub/Editor/6000.1.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath . -buildTarget StandaloneOSX
```

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
- Sentry/Crashlytics are configured under `DIAG_SENTRY` / `DIAG_CRASHLYTICS` defines.
- Unity Diagnostics (Cloud Diagnostics) initializes under `DIAG_UNITY` using UGS (`com.unity.services.core`, `com.unity.services.cloud-diagnostics`).
  - Link the project to UGS in Project Settings → Services, and set the desired environment.
  - The app sets `userId` and custom metadata (run_id, commit_sha, build_number, dev_mode, backend, platform, server_name).

## Crashlytics via Tarballs + Git LFS
- We vendor Firebase tarballs in `Packages/vendor/` with versioned filenames and track them in Git LFS.
- See `docs/CRASHLYTICS_SETUP.md` for steps to add/replace tarballs and credentials files.
- Ensure `google-services.json` (Android) and `GoogleService-Info.plist` (iOS) are present under `Assets/`.

## Contributing
- Read `AGENTS.md` for repository conventions (structure, style, tests, PRs).
- Use Conventional Commits (e.g., `feat(player): add dash`).

## Notes
- Some automation (agent CLI, programmatic crash triggers) is planned but not yet included. Track progress and open items in `requirements.md`.
