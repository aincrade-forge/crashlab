Build/Test Matrix

Supported flavors per platform (build support):

| Platform       | Sentry | Crashlytics | Unity Diagnostics | Notes                        |
|----------------|:------:|:-----------:|:-----------------:|------------------------------|
| windows-x64    |   ✓    |      —      |         ✓         | Crashlytics not on Windows   |
| macos-arm64    |   ✓    |      —      |         ✓         | Crashlytics not on macOS     |
| android-arm64  |   ✓    |      ✓      |         ✓         |                              |
| ios-arm64      |   ✓    |      ✓      |         ✓         |                              |

Tested flavors per platform (EditMode Test Matrix):

| Platform       | Sentry | Crashlytics | Unity Diagnostics | Notes                                   |
|----------------|:------:|:-----------:|:-----------------:|-----------------------------------------|
| windows-x64    |   ✓    |      —      |         ✓         |                                         |
| macos-arm64    |   ✓    |      —      |         ✓         |                                         |
| android-arm64  |   ✓    |      ✓      |         —         | Unity Diagnostics excluded from Test Matrix |
| ios-arm64      |   ✓    |      ✓      |         —         | Unity Diagnostics excluded from Test Matrix |

Build via CLI

- Single build
  - `TARGET=windows-x64 FLAVOR=sentry ./build.sh`

- Matrix builds
  - `MATRIX=true TARGETS="windows-x64,macos-arm64,android-arm64,ios-arm64" DEV_MODE=false ./build.sh`

Tests via CLI

- Matrix tests (EditMode) per flavor for current active platform
  - Desktop (Windows/macOS): `TESTS=true FLAVORS="sentry,unity" ./build.sh`
  - Mobile (Android/iOS): `TESTS=true FLAVORS="sentry,crashlytics" ./build.sh` (Unity Diagnostics not tested on mobile)
  - If FLAVORS is omitted, it defaults to all supported flavors for the active platform. On mobile this includes Unity Diagnostics; pass FLAVORS explicitly as above to exclude it.

Artifacts & Logs

- Each build writes to `Artifacts/<target>-<flavor>/`:
  - Build output (app/apk/exe or Xcode project folder)
  - `build.json` summary
  - `build.log` (Unity log for that build)

Notes

- BuildScripts switches scripting define symbols per flavor so the correct telemetry service is compiled.
- For Windows and macOS desktop targets, Firebase Crashlytics is excluded.
- For Android and iOS, Unity Cloud Diagnostics (Unity Diagnostics) is supported for builds but excluded from the Test Matrix; only Sentry and Crashlytics are exercised in tests.
- Editor helpers: CrashLab → Telemetry → Use <Provider> and Status.
