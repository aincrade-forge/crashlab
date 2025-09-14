# Sentry Setup (Unity SDK)

This project integrates the Sentry Unity SDK via UPM.

## 1) Package
- The package is referenced in `Packages/manifest.json` as:
  - `"io.sentry.unity": "https://github.com/getsentry/unity.git"`
- Open Unity and let Package Manager fetch it. To pin a version, append a tag (for example `#4.0.0`).

## 2) DSN & Options
Provide a DSN via one of the following:
- Environment variable `SENTRY_DSN`
- Unity Settings: Tools → Sentry → Setup (optional). If configured, the SDK will use those settings.

At runtime `CrashLabTelemetry` initializes Sentry with:
- `Release` set to the app version, `Environment` from `ENVIRONMENT` (default `dev`).
- User and tags (`run_id`, `backend`, `platform`, `commit_sha`, `build_number`, `dev_mode`, etc.).

## 3) Build
- Use the Editor menu: CrashLab → Build → Android/iOS • Sentry
- Or CLI with our wrapper: `TARGET=android-arm64 FLAVOR=sentry ./build.sh`

## 4) Symbols
- For accurate native/IL2CPP stack traces, upload symbols per platform. See `requirements.md` for guidance and automation notes.
