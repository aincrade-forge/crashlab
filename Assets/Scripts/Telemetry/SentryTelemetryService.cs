using System;
using System.Collections.Generic;
using UnityEngine;

#if DIAG_SENTRY
using Sentry;
using Sentry.Unity;
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
                var dsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
                SentryUnity.Init(o =>
                {
                    if (!string.IsNullOrWhiteSpace(dsn)) o.Dsn = dsn;
                    o.Release = release;
                    o.Environment = environment;
                    o.AutoSessionTracking = true;
                    o.CaptureInEditor = true;
                    o.AttachStacktrace = true;
                });

                SentrySdk.ConfigureScope(scope =>
                {
                    scope.User = new User { Id = userId };
                    foreach (var kv in meta)
                    {
                        scope.SetTag(kv.Key, kv.Value);
                    }
                });

                SentrySdk.AddBreadcrumb("CrashLab init", category: "crashlab", level: BreadcrumbLevel.Info);
                Debug.Log("CRASHLAB::SENTRY::initialized");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CrashLab Sentry init error: {e.Message}");
            }
        }

        public void OnLog(string condition, string stackTrace, LogType type)
        {
            // Sentry Unity SDK already captures Unity logs as breadcrumbs by default.
        }
    }
#endif
}
