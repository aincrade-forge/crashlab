# CrashLab Test Report — <Platform>/<Flavor>

- Date: <YYYY-MM-DD>
- Commit: `<short-sha>`
- Version: `<app version>` / Build: `<build number>`
- Device: `<model>` — OS: `<version>`
- Build Artifacts:
  - App: `<path or link>`
  - Symbols: `<path or link>`
  - Logs: `<path or link>`

## Summary

- Total actions tested: `<n>`
- Captured: `<n>` — Missing: `<n>` — N/A: `<n>`
- Notes: `<high-level notes, regressions, unexpected results>`

## Action Matrix

| Action | Group | Trigger Notes | Result | Sentry URL | Level | Symbols | Stacktrace | Notes |
|---|---|---|---|---|---|---|---|---|
| Managed: NullRef | Errors | Tap button | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Managed: DivZero | Errors | Tap button | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Managed: Unhandled | Errors | Tap button | Captured | <link> | Fatal | Managed | Good/OK/Poor | <comments> |
| Managed: Unobserved Task | Errors | Trigger + GC | Captured/Missing | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Managed: IndexOutOfRange | Errors | Tap button | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Managed: KeyNotFound | Errors | Tap button | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Managed: InvalidOperation | Errors | Modify during foreach | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Managed: AggregateException | Errors | Task.WaitAll | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Native: AccessViolation | Crashes | Native signal | Captured/Missing | <link> | Fatal | Native | Good/OK/Poor | <comments> |
| Native: Abort | Crashes | raise(SIGABRT) | Captured/Missing | <link> | Fatal | Native | Good/OK/Poor | <comments> |
| Native: FatalError | Crashes | Illegal instruction | Captured/Missing | <link> | Fatal | Native | Good/OK/Poor | <comments> |
| Native: StackOverflow | Crashes | Background thread | Captured/Missing | <link> | Fatal | Native | Good/OK/Poor | <comments> |
| Native: ForceCrash() | Crashes | Utils.ForceCrash | Captured/Missing | <link> | Fatal | Native | Good/OK/Poor | <comments> |
| Native: throw_cpp() | Crashes | Throw through C++ | Captured/Missing | <link> | Fatal | Native | Good/OK/Poor | <comments> |
| Native: crash_in_cpp() | Crashes | Null deref | Captured/Missing | <link> | Fatal | Native | Good/OK/Poor | <comments> |
| Native: crash_in_c() | Crashes | Null deref | Captured/Missing | <link> | Fatal | Native | Good/OK/Poor | <comments> |
| Native: Callback Exception | Errors | C→C# callback | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Hang: Android ANR (10s) | Errors | Sleep main thread | Captured | <link> | Error/ANR | N/A | N/A | <comments> |
| Hang: Desktop (10s) | Errors | Sleep main thread | N/A | — | — | — | — | <comments> |
| Hang: Sync Wait (10s) | Errors | Task.Delay.Wait | Captured/Missing | <link> | Error | Managed | Good/OK/Poor | <comments> |
| OOM: Heap (managed) | Crashes | Grow arrays | Captured (rethrow) | <link> | Fatal | Managed | Good/OK/Poor | <comments> |
| Android: Kotlin throw | Errors | Foreground | Captured | <link> | Error | Java | Good/OK/Poor | <comments> |
| Android: Kotlin throw (background) | Crashes | Background thread | Captured | <link> | Fatal | Java | Good/OK/Poor | <comments> |
| Android: Kotlin OOM | Errors | Foreground OOM | Captured/Missing | <link> | Fatal | Java | Good/OK/Poor | <comments> |
| Android: Kotlin OOM (background) | Crashes | Background OOM | Captured/Missing | <link> | Fatal | Java | Good/OK/Poor | <comments> |
| iOS: Objective-C throw | Errors | Foreground | Captured | <link> | Error | Native | Good/OK/Poor | <comments> |
| iOS: Native OOM | Crashes | Greedy alloc | Captured/Missing | <link> | Fatal | Native | Good/OK/Poor | <comments> |
| Memory: Asset bundle flood | Crashes | Addressables flood | Captured/Missing | <link> | Fatal | Mixed | Good/OK/Poor | <comments> |
| IO: File Write Denied | Errors | Write to root | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Data: JSON Parse Error | Errors | Invalid date parse | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Lifecycle: Use After Dispose | Errors | Read after dispose | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Thread: Background Unhandled | Errors | Throw on Thread | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Thread: ThreadPool Unhandled | Errors | Throw on TP | Captured | <link> | Error | Managed | Good/OK/Poor | <comments> |
| Thread: Unity API From Worker | Errors | Access API off main | Captured | <link> | Warning/Error | Managed | Good/OK/Poor | <comments> |
| Schedule: Startup crash | Crashes | Next launch | Captured | <link> | Fatal | Native/Managed | Good/OK/Poor | <comments> |

Legend:
- Result: Captured / Missing / N/A
- Symbols: Managed | Native | Java | Mixed
- Stacktrace: Good (file:line) / OK (partial) / Poor (addresses)

## Per‑Action Details (optional)

### <Action Label>
- Sentry issue: <link>
- Device/OS: <>
- Repro steps: <>
- Expectation: <>
- Result: Captured/Missing
- Logs: <paste or link to logcat/Player.log>
- Stacktrace/Symbols: <quality + notes>
- Comments: <analysis, regressions, hypotheses>

## Environment

| Key | Value |
|---|---|
| Flavor | `<sentry/crashlytics/unity>` |
| Platform | `<android/ios/macos/windows>` |
| Scripting Backend | `IL2CPP` |
| Build Type | `Release/Development` |
| Device | `<model>` |
| OS | `<version>` |
| DSN/Project | `<redacted>` |

## Notes / Follow‑ups

- [ ] Verify symbol upload for native crashes
- [ ] Check ANR timeout matches expectations
- [ ] Re‑run OOM cases and confirm next‑launch upload

