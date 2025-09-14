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

## Contributing
- Read `AGENTS.md` for repository conventions (structure, style, tests, PRs).
- Use Conventional Commits (e.g., `feat(player): add dash`).

## Notes
- Some automation (agent CLI, programmatic crash triggers) is planned but not yet included. Track progress and open items in `requirements.md`.
