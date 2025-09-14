Build/Test Matrix

Supported flavors per platform:

| Platform       | Sentry | Crashlytics | Unity Diagnostics | Notes                        |
|----------------|:------:|:-----------:|:-----------------:|------------------------------|
| windows-x64    |   ✓    |      —      |         ✓         | Crashlytics not on Windows   |
| macos-arm64    |   ✓    |      —      |         ✓         | Crashlytics not on macOS     |
| android-arm64  |   ✓    |      ✓      |         ✓         |                              |
| ios-arm64      |   ✓    |      ✓      |         ✓         |                              |

Build via CLI

- Single build
  - `TARGET=windows-x64 FLAVOR=sentry ./build.sh`

- Matrix builds
  - `MATRIX=true TARGETS="windows-x64,macos-arm64,android-arm64,ios-arm64" DEV_MODE=false ./build.sh`

Tests via CLI

- Matrix tests (EditMode) per flavor for current active platform
  - `TESTS=true FLAVORS="sentry,unity" ./build.sh`
  - If FLAVORS is omitted, defaults to flavors supported by the active platform.

Artifacts & Logs

- Each build writes to `Artifacts/<target>-<flavor>/`:
  - Build output (app/apk/exe or Xcode project folder)
  - `build.json` summary
  - `build.log` (Unity log for that build)

Notes

- BuildScripts switches scripting define symbols per flavor so the correct telemetry service is compiled.
- For Windows and macOS desktop targets, Firebase Crashlytics is excluded.
- Editor helpers: CrashLab → Telemetry → Use <Provider> and Status.
