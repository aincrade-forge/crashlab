using System;
using System.Collections.Generic;
using UnityEngine;

#if DIAG_SENTRY
using Sentry;
using Sentry.Unity;
using Sentry.Protocol;

#endif
namespace CrashLab
{
#if DIAG_SENTRY
    public class SentryTelemetryService : ITelemetryService
    {
        public void Initialize(string userId, IReadOnlyDictionary<string, string> meta, string release, string environment)
        {
            try
            {
                if (!SentrySdk.IsEnabled)
                {
                    var dsn = "https://744cbb7250eefb8cb1d9526d16e718ea@o4510018128904192.ingest.de.sentry.io/4510018130346064";
                    // Initialize using the Options asset as baseline and force-enable it so
                    // native backends are active at runtime as well.
                    SentryUnity.Init((SentryUnityOptions o) =>
                    {
                        o.Enabled = true;

                        if (string.IsNullOrWhiteSpace(o.Dsn))
                            o.Dsn = dsn;

                        if (string.IsNullOrEmpty(o.Release))
                            o.Release = string.IsNullOrEmpty(release) ? Application.version : release;
                        if (string.IsNullOrEmpty(o.Environment))
                            o.Environment = string.IsNullOrEmpty(environment) ? "prod" : environment;

                        o.AutoSessionTracking = true;
                        o.CaptureInEditor = true;
                        o.AttachStacktrace = true;
                        o.Debug = true; // keep on for visibility
                        o.SendDefaultPii = false;
                        o.MaxBreadcrumbs = 200;
                        o.TracesSampleRate = 0.2; // adjust if you want performance tracing

                        // Ensure native backends are toggled on; their actual inclusion is decided at build-time.
                        o.AndroidNativeSupportEnabled = true;
                        o.IosNativeSupportEnabled = true;
                        o.MacosNativeSupportEnabled = true;
                        o.WindowsNativeSupportEnabled = true;
                        o.Il2CppLineNumberSupportEnabled = true;
                    });
                    Debug.Log("CRASHLAB::SENTRY::initialized");
                }
                else
                {
                    Debug.Log("CRASHLAB::SENTRY::already_initialized (using existing settings)");
                }

                SentrySdk.ConfigureScope(scope =>
                {
                    scope.User = new SentryUser { Id = userId };
                    foreach (var kv in meta)
                    {
                        scope.SetTag(kv.Key, kv.Value);
                    }
                });

                SentrySdk.AddBreadcrumb("CrashLab init", category: "crashlab", level: BreadcrumbLevel.Info);

                EnsureSession("init");

                // No env-driven selftest; use in-app UI Sentry self-test button instead
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CrashLab Sentry init error: {e.Message}");
            }
        }

        // Env helpers removed: simplified hardcoded setup

        public void EnsureSession(string reason = null)
        {
            try
            {
                SentrySdk.StartSession();
                if (!string.IsNullOrEmpty(reason))
                {
                    SentrySdk.AddBreadcrumb($"Session started: {reason}", category: "crashlab.session", level: BreadcrumbLevel.Info);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CRASHLAB::SENTRY::SESSION_FAIL::{e.GetType().Name}:{e.Message}");
            }
        }

        public void OnLog(string condition, string stackTrace, LogType type)
        {
            try
            {
                if (type == LogType.Exception)
                {
                    // Avoid duplicate exception events: the Sentry Unity SDK captures
                    // unhandled exceptions automatically. Record a breadcrumb instead.
                    var data = string.IsNullOrEmpty(stackTrace)
                        ? null
                        : new System.Collections.Generic.Dictionary<string, string> { { "stack", stackTrace } };
                    SentrySdk.AddBreadcrumb(message: condition, category: "unity.exception", level: BreadcrumbLevel.Error, data: data);
                    return;
                }
                else
                {
                    var level = type switch
                    {
                        LogType.Error => BreadcrumbLevel.Error,
                        LogType.Assert => BreadcrumbLevel.Error,
                        LogType.Warning => BreadcrumbLevel.Warning,
                        LogType.Log => BreadcrumbLevel.Info,
                        _ => BreadcrumbLevel.Info
                    };
                    SentrySdk.AddBreadcrumb(condition, category: "unity.log", level: level);
                }
            }
            catch { /* ignore */ }
        }
    }
#endif
}
