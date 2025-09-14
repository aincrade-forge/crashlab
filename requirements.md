# Crash Lab Automation Agent — Requirements

**Last updated:** 2025-09-14  
**Document owner:** (fill in)  
**Project:** Unity Crash Lab (Sentry / Firebase Crashlytics / Unity Diagnostics)

---

## 1) Purpose & Scope

This document defines the requirements for an **automation agent** that builds, deploys, exercises, and validates the **Unity Crash Lab** application across three crash-reporting backends, and compares behavior across four platforms (all using IL2CPP):

- **Sentry (Unity SDK)**
- **Firebase Crashlytics**
- **Unity Diagnostics (Unity Dashboard)**

The agent’s goal is to produce **repeatable, apples-to-apples comparisons** between backends and platforms by ensuring **one backend is active per build**, deploying to target devices/desktops, triggering a **standard test matrix** of faults, uploading required **symbols**, and verifying ingestion where feasible.

### In scope
- Build orchestration for **iOS**, **Android**, **macOS**, and **Windows** (all IL2CPP).
- Flavor selection: **sentry**, **crashlytics**, **unity** (exactly one active per build).
- Symbol/dSYM upload automation per backend.
- Device install/launch and **programmatic crash/ANR triggers**.
- Log capture and basic verification of app-side signals.
- Optional backend verification (Sentry API / Crashlytics BigQuery if enabled).

### Out of scope
- Running multiple native crash reporters **in the same iOS binary**.
- Production release management, analytics, or feature flags beyond what’s described here.

---

## 2) Definitions

- **Agent** — The automation runner/CLI and CI workflow that performs builds, deploys, triggers faults, and verifies results.
- **Flavor** — One of `sentry`, `crashlytics`, `unity`, controlling which backend initializes.
- **Target** — Platform + architecture combination (e.g., `android-arm64`, `ios-arm64`, `macos-arm64`, `windows-x64`).
- **Event Types** — Managed exception, unobserved task exception, native crash (AccessViolation/FatalError), Android ANR.
- **Symbols** — dSYM (iOS), IL2CPP line-mapping / native symbols (Android/iOS), and Crashlytics symbol bundles (Android/iOS).

---

## 3) High-Level Requirements

### FR-1: Single-backend builds
- The agent **must** build with exactly **one** of the scripting defines: `DIAG_SENTRY`, `DIAG_CRASHLYTICS`, or `DIAG_UNITY`.
- **Do not** initialize multiple native crash reporters in the same binary (especially iOS).

### FR-2: Build matrix
- Support the following matrix (can be filtered via CLI flags):
  - Android (IL2CPP, arm64): `sentry`, `crashlytics`, `unity`
  - iOS (IL2CPP, arm64): `sentry`, `crashlytics`, `unity`
  - macOS (IL2CPP, arm64 or universal): `sentry`, `unity` (Crashlytics N/A)
  - Windows (IL2CPP, x86_64): `sentry`, `unity` (Crashlytics N/A)

### FR-3: Symbols
- Upload symbols after each build:
  - **Sentry**: Unity SDK auto-upload or `sentry-cli` fallback. Include IL2CPP line maps and native symbols:
    - Android: `.so` + IL2CPP line maps
    - iOS/macOS: dSYM bundles
    - Windows: PDBs
  - **Crashlytics** (Android/iOS only): `upload-symbols` (iOS) and Android symbol upload (Gradle task or CLI) for IL2CPP/NDK.
  - **Unity Diagnostics**: none required (handled by Unity), but keep symbols (dSYMs/PDBs/line maps) as artifacts.

### FR-4: Deployment
- Install or run the resulting artifact on the target platform.
  - Android/iOS: install to connected devices (or simulator where applicable).
  - macOS: run the built `.app` locally or on a macOS runner.
  - Windows: run the built `.exe` locally or on a Windows runner.
- iOS native crash verification requires **physical devices**; simulators are acceptable only for managed exception smoke tests.

### FR-5: Programmatic triggers (headless)
The Crash Lab app **must** support non-UI triggers so the agent can run unattended:

- **Android**: launch with an Intent extra:
  - `adb shell am start -n <pkg>/.UnityPlayerActivity -e crash_action <ACTION>`
- **iOS**: open a custom URL scheme (defined by the app):
  - `xcrun simctl openurl booted "crashlab://action/<ACTION>"` (simulator)
  - On devices, a companion tool or UI test should open the same URL scheme.
- **macOS/Windows**: pass a CLI argument or env var to the standalone player:
  - macOS: `./Builds/CrashLab.app/Contents/MacOS/CrashLab --crash_action <ACTION>`
  - Windows: `Builds\\CrashLab.exe --crash_action <ACTION>`
- Supported `<ACTION>` values (minimum set):
  - `managed_exception`
  - `unobserved_task`
  - `native_av` (AccessViolation)
  - `native_fatal` (FatalError)
  - `android_anr` (Android-only)

> The app should read these triggers on launch and execute the action immediately, logging a `CRASHLAB::<ACTION>::START` line before the action.

### FR-6: Log & telemetry capture
- Capture logs during the run and store them as build artifacts:
  - Android: `adb logcat` (filtered to `CRASHLAB::` and crash reporter tags).
  - iOS: `idevicesyslog` (physical device) or `xcrun simctl spawn booted log stream` (simulator).
  - macOS: capture Player.log at `~/Library/Logs/CompanyName/ProductName/Player.log` or stream `log stream --style compact`.
  - Windows: capture Player.log at `%USERPROFILE%\AppData\LocalLow\CompanyName\ProductName\Player.log` and process stdout/stderr.
- Mark test start/stop with unique correlation IDs (e.g., `RUN_ID` env var).

### FR-7: Verification
- **Level 0 (mandatory)**: Confirm from logs that the action ran (`CRASHLAB::<ACTION>::START`) and the app terminated when expected (for native crash/ANR tests).
- **Level 1 (Sentry)**: Use Sentry API (auth token) to assert at least one event ingested for `release`, `environment`, and `RUN_ID` tag.
- **Level 1 (Crashlytics)**: If **BigQuery export is enabled**, run a parameterized query to assert at least one matching event for the build fingerprint and `RUN_ID`. If BigQuery is not enabled, verification remains manual (agent records a console link and context).
- **Level 1 (Unity Diagnostics)**: If the Unity Dashboard exposes APIs in your org, query them; otherwise, record a console URL and context for manual verification.

### FR-8: Reporting
- Produce a run summary (JSON + Markdown) per matrix entry:
  - Build info (flavor, platform, release, versionCode/CFBundleVersion).
  - Artifact paths (APK/AAB/IPA/APP/EXE, dSYM/PDB/line maps).
  - Trigger results and timestamps.
  - Verification results (API counts, console URLs).
  - Raw log snippets around the fault window.

### FR-9: Idempotency & retries
- Safe to re-run the same job for the same commit; avoid duplicate release names by appending build numbers or git SHA.
- Retry transient operations (downloads, API calls, device flakiness).

### FR-10: Secrets hygiene
- Never print DSNs, tokens, service-account JSON, or keystore passwords in logs.
- Store secrets in CI secret manager; provide them to the agent via environment variables or short-lived files.

---

## 4) Non-Functional Requirements (NFRs)

- **NFR-1 Reliability:** The agent should tolerate intermittent device/ADB/Xcode failures with bounded retries.
- **NFR-2 Observability:** Structured logs (JSON lines) and human-friendly logs; timestamps in UTC.
- **NFR-3 Portability:** Runs on macOS build hosts (required for iOS/macOS), Windows hosts for Windows builds, and Linux/macOS hosts for Android.
- **NFR-4 Maintainability:** Config via a single YAML file; flavors and targets are data-driven.
- **NFR-5 Security:** Least-privilege credentials; no secrets written to artifacts; optional secret scanning of logs.
- **NFR-6 Traceability:** Every artifact and API call tagged with `RUN_ID`, `COMMIT_SHA`, and `RELEASE_NAME`.

---

## 5) Inputs & Configuration

### 5.1 Repository assumptions
- Unity project root contains:
  - **Editor helper scripts** that set flavor defines and stamp platform settings (Info.plist / AndroidManifest) to disable the inactive crash reporter.
  - Crash Lab scene and headless-trigger handlers (intent/URL scheme/CLI args).
- Standard Unity `Packages/`, `ProjectSettings/`, and `Assets/` (with Sentry & Firebase SDKs installed).

### 5.2 Secrets & environment variables

**Generic**
- `RUN_ID` — unique identifier per agent run (default: timestamp + short SHA).  
- `RELEASE_NAME` — e.g., `crashlab-<flavor>-<semver>+<build>`  
- `UNITY_VERSION` — e.g., `2022.3.XXf1` or `6000.0.XXf1`

**Sentry**
- `SENTRY_DSN`
- `SENTRY_AUTH_TOKEN` (for CLI/API operations)
- `SENTRY_ORG`, `SENTRY_PROJECT`

**Firebase / Crashlytics**
- `GOOGLE_SERVICES_JSON` (Android) — supplied as a file in the repo or injected by CI
- `GOOGLE_SERVICE_INFO_PLIST` (iOS) — same as above
- For Android symbol upload: service account JSON / auth token as required by your chosen upload method

**Android signing (debug is acceptable for lab)**
- `ANDROID_KEYSTORE_PATH`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASS`

**Apple codesigning (development profiles for lab)**
- One of: App Store Connect API key (recommended) or Apple ID + app-specific password, plus provisioning profiles.
- `BUNDLE_IDS` per flavor (e.g., `com.example.crashlab.sentry`, etc.).

### 5.3 Agent YAML (example)
```yaml
flavors:
  - sentry
  - crashlytics
  - unity
targets:
  - android-arm64
  - ios-arm64
  - macos-arm64
  - windows-x64
unity:
  version: 2022.3.XXf1
  scene: Assets/Scenes/CrashLab.unity
release:
  semver: 1.0.0
  buildNumber: 123
sentry:
  org: my-org
  project: crashlab
crashlytics:
  androidAppId: 1:123456:android:abcdef
  iosAppId: 1:123456:ios:abcdef
verification:
  sentryApi: true
  crashlyticsBigQuery: false
```

---

## 6) Workflows

### 6.1 Build

1. **Select flavor** → set scripting define (`DIAG_SENTRY` / `DIAG_CRASHLYTICS` / `DIAG_UNITY`).  
2. **Set bundle identifiers** per flavor and platform.  
3. **Configure version** (`PlayerSettings.bundleVersion`, `Android: versionCode`, `iOS: CFBundleVersion`).  
4. **Build**:
   - Android: IL2CPP arm64, generate **.apk** or **.aab** with Gradle.
   - iOS: Xcode project → archive → **.ipa** (development signing acceptable).
   - macOS: Standalone Player (IL2CPP) → **.app** (arm64 or universal).
   - Windows: Standalone Player (IL2CPP) → **.exe** (+ Data folder).
5. **Artifacts**: collect app packages and all symbol files (dSYM, PDBs, IL2CPP line maps, native `.so`/`sym` bundles).

### 6.2 Symbols

- **Sentry**: rely on Unity SDK auto-upload (if enabled) or run `sentry-cli upload-dif` against the build’s symbol directories.
- **Crashlytics (iOS)**: run `upload-symbols` against produced dSYM(s).
- **Crashlytics (Android)**: run appropriate symbol upload for IL2CPP/NDK output from Unity/Gradle.
- Record success/failure per upload and attach logs to the run summary.

### 6.3 Deploy & trigger

**Android**
```bash
adb install -r path/to/app.apk
# Managed exception
adb shell am start -n com.example.crashlab/.UnityPlayerActivity -e crash_action managed_exception
# Native crash
adb shell am start -n com.example.crashlab/.UnityPlayerActivity -e crash_action native_av
# ANR (Android only)
adb shell am start -n com.example.crashlab/.UnityPlayerActivity -e crash_action android_anr
```

**iOS**  
- Physical device (preferred for native crash): install IPA via `xcrun devicectl` or `ios-deploy`.  
- Trigger via custom URL scheme (device/simulator). Example:
```bash
# Simulator example (for managed crash smoke tests):
xcrun simctl install booted path/to/app.app
xcrun simctl openurl booted "crashlab://action/managed_exception"
```

**macOS**
```bash
# Run standalone player with CLI trigger
./Builds/CrashLab.app/Contents/MacOS/CrashLab --crash_action managed_exception
./Builds/CrashLab.app/Contents/MacOS/CrashLab --crash_action native_av
```

**Windows**
```powershell
# Run standalone player with CLI trigger
.\Builds\CrashLab.exe --crash_action managed_exception
.\Builds\CrashLab.exe --crash_action native_av
```

### 6.4 Log capture
- Start log capture **before** launching the app, and stop after the process terminates or a timeout elapses.
- Save logs to `artifacts/logs/<platform>-<flavor>-<RUN_ID>.log`.

### 6.5 Verification

**Sentry** (if enabled in config):
- Query events for `release == RELEASE_NAME AND tags.RUN_ID == <RUN_ID>` and count ≥ 1 for each action attempted.

**Crashlytics**:
- If BigQuery export is enabled, run a parameterized query to look for events matching `build_version` / `bundle_id` and `RUN_ID` in custom keys/breadcrumbs.
- Otherwise, include a console URL pointing to the app+version; mark verification as **manual**.

**Unity Diagnostics**:
- Include a console URL (Unity Dashboard) filtered by `version` and timestamp range; mark verification as **manual**, unless an internal API is available.

### 6.6 Report
- Emit `report.json` (machine readable) and `report.md` (human friendly) per job, plus a top-level `summary.md` across the matrix.

---

## 7) Acceptance Criteria

- **AC-1:** For each matrix entry, the agent produces a signed build artifact and collects symbols.  
- **AC-2:** For each attempted action, logs contain `CRASHLAB::<ACTION>::START`.  
- **AC-3:** For `native_*` actions, the app process terminates and logs show a fatal signal/exception.  
- **AC-4:** Symbols upload steps report success (Sentry/Crashlytics flavors).  
- **AC-5:** Sentry verification (if enabled) reports ≥ 1 event for each action.  
- **AC-6:** Crashlytics/Unity Diagnostics provide a reproducible console URL and timestamp window for manual verification when API verification is not configured.  
- **AC-7:** All artifacts and reports are attached to the CI run and retained per retention policy.

---

## 8) Constraints & Platform Notes

- **iOS:** Only one native crash reporter may be active in a build. Do **not** initialize both Crashlytics and Sentry. Do not attach a debugger when testing native crashes.  
- **Android:** Disable Crashlytics collection in non-Crashlytics flavors via manifest meta-data; ensure only the active backend initializes.  
- **macOS:** Gatekeeper/quarantine may block unsigned apps; sign locally or allow execution for lab builds. Use dSYM for symbolication.  
- **Windows:** Ensure PDBs are generated and retained; disable Windows Error Reporting UI for unattended runs if needed.  
- **IL2CPP:** Required for native crash pipelines and readable stacks with uploaded symbols across all platforms.  
- **Devices:** Keep at least one Android and one iOS physical device connected for native crash testing. Simulators are acceptable for managed-only smoke tests.  
- **Privacy:** Avoid sending PII. For Sentry, keep `SendDefaultPii=false` unless explicitly required and consented.

---

## 9) CLI Interface (proposed)

```
agent build --flavor sentry --target android-arm64 --release 1.0.0+123
agent deploy --target android-arm64 --device <serial>
agent trigger --target android-arm64 --action native_av --timeout 90
agent verify --flavor sentry --release 1.0.0+123 --run-id <RUN_ID>
agent report --out artifacts/report.md
agent run-matrix --config agent.yml
```

- The `run-matrix` command orchestrates build → symbols → deploy → trigger → verify → report for all configured combinations.

---

## 10) Error Handling & Edge Cases

- **No devices connected** → mark job as skipped with actionable message.  
- **Symbol upload fails** → continue to verification but mark the step failed; include logs.  
- **App doesn’t crash on native action** → retry once; if still no crash, mark as failure and attach logs.  
- **Crash loop on relaunch** → the app should reset the trigger flag after executing it once.  
- **Network issues** → retry API calls and package uploads with exponential backoff.

---

## 11) Security & Compliance

- Secrets stored only in CI secret store; injected at runtime as env vars or ephemeral files.  
- Logs scrubbed of tokens and DSNs.  
- Optional: enable log redaction and secret scanners as a CI step.  
- Data retention for artifacts and logs must follow company policy (default 30–90 days).

---

## 12) Deliverables

- **Agent code** (CLI or scripts) and **CI pipeline** definitions.  
- **Configuration file** (`agent.yml`) with flavors, targets, and org-specific IDs.  
- **Run artifacts:** app packages, symbols, logs, JSON & Markdown reports.  
- **Playbooks:** “How to add a new backend” and “How to onboard a new device”.

---

## 13) Appendix A — Example GitHub Actions outline

```yaml
name: Crash Lab Matrix
on: [workflow_dispatch, push]

jobs:
  build-test:
    runs-on: macos-latest
    strategy:
      fail-fast: false
      matrix:
        target: [android-arm64, ios-arm64, macos-arm64, windows-x64]
        flavor: [sentry, crashlytics, unity]

    steps:
      - uses: actions/checkout@v4

      # (Optional) Set up Unity Editor
      - name: Unity - Activate & Cache
        uses: game-ci/unity-activate@v2
        with: { unityVersion: ${{ env.UNITY_VERSION }} }

      - name: Build
        run: |
          ./agent build --flavor ${{ matrix.flavor }} --target ${{ matrix.target }} --release "${RELEASE_NAME}"

      - name: Symbols
        run: ./agent symbols --flavor ${{ matrix.flavor }} --target ${{ matrix.target }}

      - name: Deploy & Trigger (Android)
        if: matrix.target == 'android-arm64'
        run: |
          ./agent deploy --target android-arm64
          ./agent trigger --target android-arm64 --action managed_exception
          ./agent trigger --target android-arm64 --action native_av
          ./agent trigger --target android-arm64 --action android_anr

      - name: Deploy & Trigger (iOS)
        if: matrix.target == 'ios-arm64'
        run: |
          ./agent deploy --target ios-arm64
          ./agent trigger --target ios-arm64 --action managed_exception
          ./agent trigger --target ios-arm64 --action native_av
          ./agent trigger --target ios-arm64 --action native_fatal

      - name: Deploy & Trigger (macOS)
        if: matrix.target == 'macos-arm64'
        run: |
          ./agent deploy --target macos-arm64
          ./agent trigger --target macos-arm64 --action managed_exception
          ./agent trigger --target macos-arm64 --action native_av

      - name: Deploy & Trigger (Windows)
        if: matrix.target == 'windows-x64'
        runs-on: windows-latest
        shell: bash
        run: |
          ./agent deploy --target windows-x64
          ./agent trigger --target windows-x64 --action managed_exception
          ./agent trigger --target windows-x64 --action native_av

      - name: Verify
        run: ./agent verify --flavor ${{ matrix.flavor }} --release "${RELEASE_NAME}" --run-id "${RUN_ID}"

      - name: Report
        run: ./agent report --out artifacts/${{ matrix.target }}-${{ matrix.flavor }}-report.md
```

> Note: The above is an outline; you can replace `./agent ...` with bash scripts or Makefile targets if preferred. Use separate jobs or conditional `runs-on` for Windows steps when building/running Windows targets.

---

## 14) Appendix B — Example BigQuery verification (optional)

If Crashlytics → BigQuery export is enabled, the agent can run a query like this (illustrative only; adjust table/fields per your export schema):

```sql
SELECT
  event_timestamp,
  issue_id,
  fatal,
  app_info.bundle_id,
  app_info.version_name,
  ARRAY_TO_STRING(ARRAY(SELECT kv.value.string_value
                        FROM UNNEST(custom_keys) kv
                        WHERE kv.key = 'RUN_ID'), '') AS run_id
FROM `your_project.crashlytics.your_table`
WHERE app_info.bundle_id = @bundle_id
  AND app_info.version_name = @version_name
  AND TIMESTAMP(event_timestamp) BETWEEN @t_start AND @t_end
  AND EXISTS (
      SELECT 1 FROM UNNEST(custom_keys) kv
      WHERE kv.key = 'RUN_ID' AND kv.value.string_value = @run_id
  )
ORDER BY event_timestamp DESC
LIMIT 50;
```

---

## 15) Open Items (fill in before implementation)

- Bundle IDs and Firebase app IDs per flavor.  
- Sentry org/project and release naming convention.  
- Device inventory and attachment strategy (USB hub, device farm, etc.).  
- Whether Crashlytics BigQuery export will be enabled.  
- Desired retention for logs and artifacts.  

---

## 16) Change Log

- **2025-09-14** — Initial version; updated to include platform comparison across iOS, Android, macOS, and Windows (IL2CPP).
