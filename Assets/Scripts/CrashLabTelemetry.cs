using System;
using System.Collections.Generic;
using UnityEngine;

#if DIAG_SENTRY
using Sentry;
#endif

#if DIAG_CRASHLYTICS
using Firebase;
using Firebase.Extensions;
using Firebase.Crashlytics;
#endif

#if DIAG_UNITY
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.CloudDiagnostics; // CrashReporting API lives here
#endif

namespace CrashLab
{
    public static class CrashLabTelemetry
    {
        private static bool _initialized;
        private static readonly Dictionary<string, string> Meta = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Resolve metadata from env or defaults
            var runId = GetEnv("RUN_ID", Guid.NewGuid().ToString("N"));
            var release = GetEnv("RELEASE_NAME", Application.version);
            var environment = GetEnv("ENVIRONMENT", "dev");
            var commitSha = GetEnv("COMMIT_SHA", "local");
            var buildNumber = GetEnv("BUILD_NUMBER", Application.buildGUID);
            var devMode = GetEnv("DEV_MODE", Debug.isDebugBuild ? "true" : "false");
            var ci = GetEnv("CI", "false");
            var serverName = GetEnv("SERVER_NAME", SystemInfo.deviceName);
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

            var userId = GetEnv("USER_ID", LoadOrCreateUserId());

            Meta["run_id"] = runId;
            Meta["release"] = release;
            Meta["environment"] = environment;
            Meta["backend"] = backend;
            Meta["platform"] = platform;
            Meta["commit_sha"] = commitSha;
            Meta["build_number"] = buildNumber;
            Meta["dev_mode"] = devMode;
            Meta["ci"] = ci;
            Meta["server_name"] = serverName;
            Meta["user_id"] = userId;
            Meta["app_start_ts"] = DateTime.UtcNow.ToString("o");

            // Apply to active backend
#if DIAG_SENTRY
            try
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.User = new User { Id = userId };
                    SetSentryTags(scope, Meta);
                });
                SentrySdk.AddBreadcrumb($"CrashLab init run_id={runId}", category: "crashlab", level: BreadcrumbLevel.Info);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CrashLabTelemetry(Sentry) init error: {e.Message}");
            }
#elif DIAG_CRASHLYTICS
            InitializeCrashlyticsAsync(userId, Meta);
#elif DIAG_UNITY
            try
            {
                InitializeUnityDiagnosticsAsync(environment, userId, Meta);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CrashLabTelemetry(UnityDiag) init error: {e.Message}");
            }
#else
            Debug.Log($"CRASHLAB::META::{ToKvpString(Meta)}");
#endif

            Application.logMessageReceived += OnLog;
            Debug.Log($"CRASHLAB::INIT::run_id={runId}");
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
#if DIAG_SENTRY
            var level = BreadcrumbLevel.Info;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    level = BreadcrumbLevel.Error; break;
                case LogType.Warning:
                    level = BreadcrumbLevel.Warning; break;
                default:
                    level = BreadcrumbLevel.Info; break;
            }
            SentrySdk.AddBreadcrumb(condition, category: "unity.log", level: level);
#elif DIAG_CRASHLYTICS
            Crashlytics.Log(condition);
#elif DIAG_UNITY
            // Cloud Diagnostics has no breadcrumbs API; logs + metadata provide context.
#else
            // Nothing extra; logs already in Player.log
#endif
        }

#if DIAG_SENTRY
        private static void SetSentryTags(Scope scope, IReadOnlyDictionary<string, string> dict)
        {
            foreach (var kv in dict)
            {
                // Prefer tags so they show up prominently
                scope.SetTag(kv.Key, kv.Value);
            }
        }
#endif

        private static string LoadOrCreateUserId()
        {
            const string key = "crashlab_user_id";
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetString(key);
            }
            var id = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(key, id);
            PlayerPrefs.Save();
            return id;
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

#if DIAG_UNITY
        private static async void InitializeUnityDiagnosticsAsync(string environment, string userId, IReadOnlyDictionary<string, string> meta)
        {
            try
            {
                var options = new InitializationOptions().SetEnvironmentName(environment);
                await UnityServices.InitializeAsync(options);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"UnityServices.InitializeAsync failed: {e.Message}");
                // continue; Cloud Diagnostics may still be unavailable
            }

            try
            {
                // UGS Cloud Diagnostics Crash Reporting API
                UnityEngine.CrashReportHandler.CrashReportHandler.SetUserMetadata("user_id", userId);
                foreach (var kv in meta) 
                {
                  UnityEngine.CrashReportHandler.CrashReportHandler.SetUserMetadata(kv.Key, kv.Value);
                }
                Debug.Log("CRASHLAB::UNITY_DIAGNOSTICS::initialized");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CloudDiagnostics CrashReporting init failed: {e.Message}");
            }
        }
#endif

#if DIAG_CRASHLYTICS
        private static async void InitializeCrashlyticsAsync(string userId, IReadOnlyDictionary<string, string> meta)
        {
            try
            {
                var status = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (status != DependencyStatus.Available)
                {
                    Debug.LogWarning($"Firebase dependencies not available: {status}");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Firebase CheckAndFixDependenciesAsync failed: {e.Message}");
                return;
            }

            try
            {
                Crashlytics.IsCrashlyticsCollectionEnabled = true;
                Crashlytics.SetUserId(userId);
                foreach (var kv in meta)
                {
                    Crashlytics.SetCustomKey(kv.Key, kv.Value);
                }
                Crashlytics.Log("CrashLab Crashlytics initialized");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Crashlytics init failed: {e.Message}");
            }
        }
#endif
    }
}
