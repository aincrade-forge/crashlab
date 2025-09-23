Sentry Unity Native Support (Manual Setup)

This project initializes Sentry in code (no Options asset). Follow this checklist to ensure native crash capture works across platforms.

Initialization
- We call `SentryUnity.Init` at app start (BeforeSceneLoad) and set Release/Environment, user, and tags.
- Options are configurable via environment variables, including generic `SENTRY_OPT_<Property>` that maps to any `SentryUnityOptions` property.
  - Examples: `SENTRY_OPT_Debug=true`, `SENTRY_OPT_SendDefaultPii=true`, `SENTRY_OPT_IosNativeSupportEnabled=true`.

Build settings
- Scripting Backend: IL2CPP on all platforms (BuildScripts enforces this).
- Architectures:
  - Android: ARM64 (default; BuildScripts sets `Android.targetArchitectures = ARM64`).
  - iOS: ARM64.
  - Desktop: Standard IL2CPP setup.

Symbols and debug info
- Android (IL2CPP): keep native `.so` and IL2CPP line-mapping files; upload via `scripts/sentry_upload_symbols.sh`.
- iOS/macOS: generate dSYMs; upload via the same script after Xcode archive (iOS) or post‑build (macOS).
- Windows: keep PDBs and upload via the script.

Options to consider (via env or `SENTRY_OPT_*`)
- Native support toggles per platform (property names per Sentry 3.4.0):
  - `AndroidNativeSupportEnabled`
  - `IosNativeSupportEnabled`
  - `MacOsNativeSupportEnabled`
  - `WindowsNativeSupportEnabled`
- ANR detection (Android): check and adjust if available in your SDK version (e.g., `AnrDetectionEnabled` / `AnrTimeout`).
- Performance sampling: `SENTRY_TRACES_SAMPLE_RATE` (0–1), Profiles if supported.
- Privacy: `SENTRY_SEND_DEFAULT_PII`.
- In-app classification: `SENTRY_INAPP_INCLUDE`, `SENTRY_INAPP_EXCLUDE`.

Env examples
```
SENTRY_DSN=... RELEASE_NAME=1.0.0 ENVIRONMENT=prod \
SENTRY_DEBUG=false SENTRY_SEND_DEFAULT_PII=false \
SENTRY_TRACES_SAMPLE_RATE=0.2 \
SENTRY_INAPP_INCLUDE=CrashLab,MyGame \
SENTRY_OPT_AndroidNativeSupportEnabled=true \
SENTRY_OPT_IosNativeSupportEnabled=true \
SENTRY_OPT_MacOsNativeSupportEnabled=true \
SENTRY_OPT_WindowsNativeSupportEnabled=true
```

Verification matrix
- Managed crash (Unhandled): should appear with full managed stack.
- Native crash (AccessViolation): should appear with native stack; symbolication requires uploaded symbols.
- Android ANR: should appear as ANR event when native support enabled.
- OOM: varies by platform; iOS often reports OOM sessions.

CI integration
- Symbol upload is handled by the Sentry Unity SDK when "Upload Symbols" is enabled in `SentryOptions.asset`.
- For iOS, upload after Xcode archive; for Windows, call the script with `PDB_DIR`.
