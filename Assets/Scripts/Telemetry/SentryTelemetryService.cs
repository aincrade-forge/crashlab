using System;
using System.Collections.Generic;
using System.Collections;
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
                if (!SentrySdk.IsEnabled)
                {
                    var dsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
                    SentryUnity.Init(o =>
                    {
                        if (!string.IsNullOrWhiteSpace(dsn)) o.Dsn = dsn;
                        o.Release = release;
                        o.Environment = environment;
                        // Sensible defaults (can override via env vars below)
                        o.AutoSessionTracking = true;
                        o.CaptureInEditor = true;
                        o.AttachStacktrace = true;

                        // Optional advanced configuration via env vars
                        ApplyEnvOverrides(o);
                    });
                    Debug.Log("CRASHLAB::SENTRY::initialized");
                }
                else
                {
                    Debug.Log("CRASHLAB::SENTRY::already_initialized (using existing settings)");
                }

                SentrySdk.ConfigureScope(scope =>
                {
                    scope.User = new User { Id = userId };
                    foreach (var kv in meta)
                    {
                        scope.SetTag(kv.Key, kv.Value);
                    }
                });

                SentrySdk.AddBreadcrumb("CrashLab init", category: "crashlab", level: BreadcrumbLevel.Info);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CrashLab Sentry init error: {e.Message}");
            }
        }

        private static void ApplyEnvOverrides(SentryUnityOptions o)
        {
            // Booleans
            if (TryGetBool("SENTRY_DEBUG", out var debug)) o.Debug = debug;
            if (TryGetBool("SENTRY_AUTO_SESSION_TRACKING", out var ast)) o.AutoSessionTracking = ast;
            if (TryGetBool("SENTRY_CAPTURE_IN_EDITOR", out var cie)) o.CaptureInEditor = cie;
            if (TryGetBool("SENTRY_ATTACH_STACKTRACE", out var attach)) o.AttachStacktrace = attach;
            if (TryGetBool("SENTRY_SEND_DEFAULT_PII", out var pii)) o.SendDefaultPii = pii;

            // Numbers
            if (TryGetDouble("SENTRY_TRACES_SAMPLE_RATE", out var tsr)) o.TracesSampleRate = tsr;
            // ProfilesSampleRate may not be present in all SDK versions; reflection fallback provided below.
            if (TryGetDouble("SENTRY_PROFILES_SAMPLE_RATE", out var psr))
            {
                var prop = typeof(SentryUnityOptions).GetProperty("ProfilesSampleRate");
                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(double))
                {
                    prop.SetValue(o, psr);
                }
            }
            if (TryGetInt("SENTRY_MAX_BREADCRUMBS", out var maxBc)) o.MaxBreadcrumbs = maxBc;

            // Strings
            var serverName = Environment.GetEnvironmentVariable("SENTRY_SERVER_NAME");
            if (!string.IsNullOrWhiteSpace(serverName)) o.ServerName = serverName;

            var inAppIncludes = Environment.GetEnvironmentVariable("SENTRY_INAPP_INCLUDE");
            if (!string.IsNullOrWhiteSpace(inAppIncludes))
            {
                foreach (var part in inAppIncludes.Split(','))
                {
                    var p = part.Trim();
                    if (!string.IsNullOrEmpty(p)) o.InAppInclude.Add(p);
                }
            }

            var inAppExcludes = Environment.GetEnvironmentVariable("SENTRY_INAPP_EXCLUDE");
            if (!string.IsNullOrWhiteSpace(inAppExcludes))
            {
                foreach (var part in inAppExcludes.Split(','))
                {
                    var p = part.Trim();
                    if (!string.IsNullOrEmpty(p)) o.InAppExclude.Add(p);
                }
            }

            // Generic overrides: any SentryUnityOptions property can be set via SENTRY_OPT_<PropertyName>
            ApplyArbitraryEnvOverrides(o);
        }

        private static void ApplyArbitraryEnvOverrides(SentryUnityOptions o)
        {
            try
            {
                var vars = Environment.GetEnvironmentVariables();
                foreach (DictionaryEntry entry in vars)
                {
                    if (entry.Key is not string key) continue;
                    if (!key.StartsWith("SENTRY_OPT_", StringComparison.Ordinal)) continue;
                    var propName = key.Substring("SENTRY_OPT_".Length);
                    var valueString = entry.Value as string;
                    if (string.IsNullOrEmpty(propName) || valueString == null) continue;

                    var prop = typeof(SentryUnityOptions).GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (prop == null || !prop.CanWrite) continue;

                    object parsed = null;
                    var t = prop.PropertyType;
                    try
                    {
                        if (t == typeof(string)) parsed = valueString;
                        else if (t == typeof(bool)) parsed = bool.Parse(valueString);
                        else if (t == typeof(int)) parsed = int.Parse(valueString);
                        else if (t == typeof(double)) parsed = double.Parse(valueString, System.Globalization.CultureInfo.InvariantCulture);
                        else if (t.IsEnum) parsed = Enum.Parse(t, valueString, ignoreCase: true);
                        else continue;
                    }
                    catch
                    {
                        continue; // skip invalid values
                    }

                    prop.SetValue(o, parsed);
                }
            }
            catch
            {
                // Ignore when env enumeration unsupported
            }
        }

        private static bool TryGetBool(string key, out bool value)
        {
            value = false;
            var s = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(s)) return false;
            return bool.TryParse(s, out value);
        }

        private static bool TryGetDouble(string key, out double value)
        {
            value = 0;
            var s = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(s)) return false;
            return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetInt(string key, out int value)
        {
            value = 0;
            var s = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(s)) return false;
            return int.TryParse(s, out value);
        }

        public void OnLog(string condition, string stackTrace, LogType type)
        {
            // Sentry Unity SDK already captures Unity logs as breadcrumbs by default.
        }
    }
#endif
}
