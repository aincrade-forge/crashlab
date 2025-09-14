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

## 5) Verification
- Capture a test message from the Editor: CrashLab → Sentry → Capture Test Message.
- Trigger a crash via in‑game UI or headless triggers and verify the event.
- Scripted check (requires `sentry-cli`):
```
RUN_ID=demo-run SENTRY_ORG=<org> SENTRY_PROJECT=<project> SENTRY_AUTH_TOKEN=<token> \
  ./scripts/sentry_verify.sh
```

## 6) Auto Symbol Uploads
- The post‑build hook attempts to upload symbols by default:
  - macOS/Android → uploads when Sentry env is configured.
  - iOS → upload after Xcode archive using `scripts/sentry_upload_symbols.sh`.
- Opt‑out: set `CRASHLAB_NO_UPLOAD_SYMBOLS=true` before building.
