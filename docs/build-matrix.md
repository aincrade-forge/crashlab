Build/Test Matrix

Targets and supported telemetry flavors:

- windows-x64
  - sentry
  - unity
  - crashlytics: not supported

- macos-arm64
  - sentry
  - unity
  - crashlytics: not supported

- android-arm64
  - sentry
  - crashlytics
  - unity

- ios-arm64
  - sentry
  - crashlytics
  - unity

Build via CLI

- Single build
  - TARGET=windows-x64 FLAVOR=sentry ./build.sh

- Matrix builds
  - MATRIX=true TARGETS="windows-x64,macos-arm64,android-arm64,ios-arm64" DEV_MODE=false ./build.sh

Tests via CLI

- Matrix tests (EditMode) per flavor for current active platform
  - TESTS=true FLAVORS="sentry,unity" ./build.sh
  - If FLAVORS is omitted, defaults to flavors supported by the active platform.

Notes

- BuildScripts switches scripting define symbols per flavor so the correct telemetry service is compiled.
- For Windows and macOS desktop targets, Firebase Crashlytics is excluded.
