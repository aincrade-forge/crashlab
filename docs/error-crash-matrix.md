Crash/Error Coverage Matrix

Legend
- ✓ Expected capture
- ~ May capture or partially supported (see notes)
- — Not expected to capture

Assumptions
- Symbols uploaded for the target (DSYM/PDB/line maps) where applicable.
- One provider active per build via DIAG_* define.

Android (android-arm64)

| Trigger                     | Sentry | Crashlytics | Unity Diagnostics | Notes |
|----------------------------|:------:|:-----------:|:-----------------:|-------|
| Managed: NullRef/DivZero   |   ✓    |      ✓      |         ✓         | Unhandled exceptions
| Managed: Unhandled         |   ✓    |      ✓      |         ✓         | throw new Exception
| Managed: Unobserved Task   |   ~    |      ~      |         ~         | Delivery varies; not always fatal
| Native: AccessViolation    |   ✓    |      ✓      |         ✓         | ForceCrash AccessViolation
| Native: Abort              |   ✓    |      ✓      |         ✓         | ForceCrash Abort
| Native: FatalError         |   ✓    |      ✓      |         ✓         | ForceCrash FatalError
| Native: StackOverflow      |   ✓    |      ✓      |         ✓         | Deep recursion
| Hang: Android ANR (10s)    |   ✓    |      ✓      |         —         | Crashlytics & Sentry detect ANR
| Hang: Desktop (10s)        |   —    |      —      |         —         | Not applicable on Android
| OOM: Heap                  |   ~    |      ~      |         ~         | Often process-kill; capture varies
| Startup: Managed Unhandled |   ✓    |      ✓      |         ✓         | Triggered at app start

iOS (ios-arm64)

| Trigger                     | Sentry | Crashlytics | Unity Diagnostics | Notes |
|----------------------------|:------:|:-----------:|:-----------------:|-------|
| Managed: NullRef/DivZero   |   ✓    |      ✓      |         ✓         | Unhandled exceptions
| Managed: Unhandled         |   ✓    |      ✓      |         ✓         | throw new Exception
| Managed: Unobserved Task   |   ~    |      ~      |         ~         | Delivery varies; not always fatal
| Native: AccessViolation    |   ✓    |      ✓      |         ✓         | ForceCrash AccessViolation
| Native: Abort              |   ✓    |      ✓      |         ✓         | ForceCrash Abort
| Native: FatalError         |   ✓    |      ✓      |         ✓         | ForceCrash FatalError
| Native: StackOverflow      |   ✓    |      ✓      |         ✓         | Deep recursion
| Hang: Android ANR (10s)    |   —    |      —      |         —         | Not applicable on iOS
| Hang: Desktop (10s)        |   —    |      —      |         —         | Use for macOS/Windows only
| OOM: Heap                  |   ~    |      ✓      |         ~         | Crashlytics reports iOS OOMs
| Startup: Managed Unhandled |   ✓    |      ✓      |         ✓         | Triggered at app start

macOS (macos-arm64)

| Trigger                     | Sentry | Crashlytics | Unity Diagnostics | Notes |
|----------------------------|:------:|:-----------:|:-----------------:|-------|
| Managed: NullRef/DivZero   |   ✓    |      —      |         ✓         | Crashlytics not in matrix for macOS
| Managed: Unhandled         |   ✓    |      —      |         ✓         | throw new Exception
| Managed: Unobserved Task   |   ~    |      —      |         ~         | Delivery varies; not always fatal
| Native: AccessViolation    |   ✓    |      —      |         ✓         | ForceCrash AccessViolation
| Native: Abort              |   ✓    |      —      |         ✓         | ForceCrash Abort
| Native: FatalError         |   ✓    |      —      |         ✓         | ForceCrash FatalError
| Native: StackOverflow      |   ✓    |      —      |         ✓         | Deep recursion
| Hang: Android ANR (10s)    |   —    |      —      |         —         | Not applicable on macOS
| Hang: Desktop (10s)        |   —    |      —      |         —         | Hangs aren’t reported as crashes
| OOM: Heap                  |   ~    |      —      |         ~         | Process-kill; capture varies
| Startup: Managed Unhandled |   ✓    |      —      |         ✓         | Triggered at app start

Windows (windows-x64)

| Trigger                     | Sentry | Crashlytics | Unity Diagnostics | Notes |
|----------------------------|:------:|:-----------:|:-----------------:|-------|
| Managed: NullRef/DivZero   |   ✓    |      —      |         ✓         | Crashlytics not in matrix for Windows
| Managed: Unhandled         |   ✓    |      —      |         ✓         | throw new Exception
| Managed: Unobserved Task   |   ~    |      —      |         ~         | Delivery varies; not always fatal
| Native: AccessViolation    |   ✓    |      —      |         ✓         | ForceCrash AccessViolation
| Native: Abort              |   ✓    |      —      |         ✓         | ForceCrash Abort
| Native: FatalError         |   ✓    |      —      |         ✓         | ForceCrash FatalError
| Native: StackOverflow      |   ✓    |      —      |         ✓         | Deep recursion
| Hang: Android ANR (10s)    |   —    |      —      |         —         | Not applicable on Windows
| Hang: Desktop (10s)        |   —    |      —      |         —         | Hangs aren’t reported as crashes
| OOM: Heap                  |   ~    |      —      |         ~         | Process-kill; capture varies
| Startup: Managed Unhandled |   ✓    |      —      |         ✓         | Triggered at app start

General notes
- Unobserved Task exceptions may be raised on finalizer or background threads and aren’t guaranteed to crash the app; capture depends on SDK behavior and platform.
- Heap OOM often terminates the process without an exception path; iOS Crashlytics reports OOM sessions; others may not capture consistently.
- Desktop hang is for manual evaluation of watchdog behavior; it’s not a crash and isn’t reported by crash SDKs.
- Android ANR is reported by Crashlytics and Sentry Android; Unity Cloud Diagnostics does not report ANR.
- Ensure symbol uploads are enabled for accurate native stack traces.

