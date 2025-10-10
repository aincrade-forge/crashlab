using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrashLab
{
    public static class CrashLabTelemetry
    {
        private static bool _initialized;
        private static readonly Dictionary<string, string> Meta = new();
        private static ITelemetryService _service;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Resolve metadata from env or defaults
            var runId = GetEnv("RUN_ID", "demo-run");
            var release = GetEnv("RELEASE_NAME", Application.version);
            var environment = GetEnv("ENVIRONMENT", "dev");
            var commitSha = GetEnv("COMMIT_SHA", null);
            var buildNumber = GetEnv("BUILD_NUMBER", "100");
            var devMode = GetEnv("DEV_MODE", "true");
            var ci = GetEnv("CI", "false");
            var serverName = GetEnv("SERVER_NAME", "demo-machine");
            var backend =
#if DIAG_SENTRY
                "sentry";
#elif DIAG_CRASHLYTICS
                "crashlytics";
#elif DIAG_UNITY
                "unity";
#else
                "unknown";
#endif

            var platform = Application.platform.ToString().ToLowerInvariant();

            var userId = GetEnv("USER_ID", "jin");

            Meta["run_id"] = runId;
            Meta["release"] = release;
            Meta["environment"] = environment;
            Meta["backend"] = backend;
            Meta["platform"] = platform;
            // Optionally override commit sha and build info from embedded build info asset
            try
            {
                var info = Resources.Load<CrashLabBuildInfo>("CrashLabBuildInfo");
                if (info != null)
                {
                    if (string.IsNullOrEmpty(commitSha)) commitSha = info.commitSha;
                    if (!string.IsNullOrEmpty(info.buildNumber)) buildNumber = info.buildNumber;
                    Meta["build_ts"] = string.IsNullOrEmpty(info.buildTimestampUtc)
                        ? DateTime.UtcNow.ToString("o")
                        : info.buildTimestampUtc;
                    if (!string.IsNullOrEmpty(info.branch)) Meta["branch"] = info.branch;
                }
            }
            catch { /* ignore if resources not present */ }

            Meta["commit_sha"] = string.IsNullOrEmpty(commitSha) ? "unknown" : commitSha;
            Meta["build_number"] = buildNumber;
            Meta["dev_mode"] = devMode;
            Meta["ci"] = ci;
            Meta["server_name"] = serverName;
            Meta["user_id"] = userId;
            Meta["app_start_ts"] = DateTime.UtcNow.ToString("o");

            // Resolve and initialize telemetry service
            _service = CreateService();
            _service.Initialize(userId, Meta, release, environment);

            // Control Unity's built-in CrashReportHandler capture via code.
            // By default we keep it ON for DIAG_UNITY builds, and OFF otherwise to avoid duplicates.
            // You can override in non-Unity flavors by setting env UNITY_CAPTURE_EXCEPTIONS=true.
#if DIAG_UNITY
            try { UnityEngine.CrashReportHandler.CrashReportHandler.enableCaptureExceptions = true; } catch { }
#else
            var unityCapture = GetEnv("UNITY_CAPTURE_EXCEPTIONS", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
            try { UnityEngine.CrashReportHandler.CrashReportHandler.enableCaptureExceptions = unityCapture; } catch { }
#endif
            try
            {
                Debug.Log($"CRASHLAB::UNITY_CRASH_HANDLER::enabled={UnityEngine.CrashReportHandler.CrashReportHandler.enableCaptureExceptions}");
            }
            catch { }

            Application.logMessageReceived += OnLog;
            Debug.Log($"CRASHLAB::INIT::run_id={runId}");
        }

        public static void EnsureSession(string reason = null)
        {
            if (_service == null)
            {
                Debug.LogWarning("CrashLabTelemetry session requested before initialization");
                return;
            }

            try
            {
                _service.EnsureSession(reason);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CrashLabTelemetry session start failed: {e.Message}");
            }
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            _service?.OnLog(condition, stackTrace, type);
        }

        private static string GetEnv(string key, string fallback)
            => Environment.GetEnvironmentVariable(key) ?? fallback;

        private static string ToKvpString(IReadOnlyDictionary<string, string> dict)
        {
            var parts = new List<string>(dict.Count);
            foreach (var kv in dict)
            {
                parts.Add($"{kv.Key}={kv.Value}");
            }
            return string.Join(",", parts);
        }

        private static ITelemetryService CreateService()
        {
#if DIAG_SENTRY
            return new SentryTelemetryService();
#elif DIAG_CRASHLYTICS
            return new CrashlyticsTelemetryService();
#elif DIAG_UNITY
            return new UnityDiagnosticsTelemetryService();
#else
            return new NoTelemetryService();
#endif
        }
    }
}
