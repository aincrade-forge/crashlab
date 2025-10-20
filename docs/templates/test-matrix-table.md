# CrashLab Cross‑Platform Test Matrix

Fill each cell with a Sentry/Crashlytics issue link (or N/A), and add any quick notes. “Group” is the expected category in our UI (Errors vs Crashes).

| Action | Group | Android (Sentry) | Android (Firebase) | iOS (Sentry) | iOS (Firebase) | macOS (Sentry) | macOS (Unity) | Windows (Sentry) | Windows (Unity) | Notes |
|---|---|---|---|---|---|---|---|---|---|---|
| Managed: NullRef | Errors |  |  |  |  |  |  |  |  |  |
| Managed: DivZero | Errors |  |  |  |  |  |  |  |  |  |
| Managed: Unhandled | Errors |  |  |  |  |  |  |  |  |  |
| Managed: Unobserved Task | Errors |  |  |  |  |  |  |  |  |  |
| Managed: IndexOutOfRange | Errors |  |  |  |  |  |  |  |  |  |
| Managed: KeyNotFound | Errors |  |  |  |  |  |  |  |  |  |
| Managed: InvalidOperation (Modify During Enum) | Errors |  |  |  |  |  |  |  |  |  |
| Managed: AggregateException | Errors |  |  |  |  |  |  |  |  |  |
| Native: AccessViolation | Crashes |  |  |  |  |  |  |  |  |  |
| Native: Abort | Crashes |  |  |  |  |  |  |  |  |  |
| Native: FatalError | Crashes |  |  |  |  |  |  |  |  |  |
| Native: StackOverflow | Crashes |  |  |  |  |  |  |  |  |  |
| Native: ForceCrash() | Crashes |  |  |  |  |  |  |  |  |  |
| Native: throw_cpp() | Crashes |  |  |  |  |  |  |  |  |  |
| Native: crash_in_cpp() | Crashes |  |  |  |  |  |  |  |  |  |
| Native: crash_in_c() | Crashes |  |  |  |  |  |  |  |  |  |
| Native: Callback Exception | Errors |  |  |  |  |  |  |  |  |  |
| Hang: Android ANR (10s) | Errors |  |  |  |  |  |  |  |  |  |
| Hang: Desktop (10s) | Errors |  |  |  |  |  |  |  |  |  |
| Hang: Sync Wait (10s) | Errors |  |  |  |  |  |  |  |  |  |
| OOM: Heap (managed) | Crashes |  |  |  |  |  |  |  |  |  |
| Android: Kotlin throw | Errors |  |  |  |  |  |  |  |  |  |
| Android: Kotlin throw (background) | Crashes |  |  |  |  |  |  |  |  |  |
| Android: Kotlin OOM | Errors |  |  |  |  |  |  |  |  |  |
| Android: Kotlin OOM (background) | Crashes |  |  |  |  |  |  |  |  |  |
| iOS: Objective‑C throw | Errors |  |  |  |  |  |  |  |  |  |
| iOS: Native OOM | Crashes |  |  |  |  |  |  |  |  |  |
| Memory: Asset bundle flood | Crashes |  |  |  |  |  |  |  |  |  |
| Memory: Asset bundle flood (editor) | Errors |  |  |  |  |  |  |  |  |  |
| IO: File Write Denied | Errors |  |  |  |  |  |  |  |  |  |
| Data: JSON Parse Error | Errors |  |  |  |  |  |  |  |  |  |
| Lifecycle: Use After Dispose | Errors |  |  |  |  |  |  |  |  |  |
| Thread: Background Unhandled | Errors |  |  |  |  |  |  |  |  |  |
| Thread: ThreadPool Unhandled | Errors |  |  |  |  |  |  |  |  |  |
| Thread: Unity API From Worker | Errors |  |  |  |  |  |  |  |  |  |
| Schedule: Startup crash | Crashes |  |  |  |  |  |  |  |  |  |

Notes:
- Put full issue URLs in the cells (or “N/A”).
- If a case is expected to upload only on next launch (e.g., crashes/ANR), note it in the Notes column.
- Use “Unity” columns for Unity Diagnostics builds on desktop.

