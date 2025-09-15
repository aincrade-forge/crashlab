using System;
using System.Collections.Generic;
using System.Collections;
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
                    SentrySdk.Init(o =>
                    {
                        if (!string.IsNullOrWhiteSpace(dsn)) o.Dsn = dsn;
                        o.Release = release;
                        o.Environment = environment;
                        // Sensible defaults (can override via env vars below)
                        o.AutoSessionTracking = true;
                        // o.CaptureInEditor = true;
                        o.AttachStacktrace = true;

                        // Ensure native support defaults are enabled where available
                        ApplyNativeDefaults(o);

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

                var selftest = Environment.GetEnvironmentVariable("SENTRY_SELFTEST");
                if (!string.IsNullOrEmpty(selftest) && selftest.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        meta.TryGetValue("run_id", out var rid);
                        SentrySdk.CaptureMessage($"CrashLab Sentry selftest run_id={rid ?? "unknown"}");
                        SentrySdk.Flush(TimeSpan.FromSeconds(2));
                        Debug.Log("CRASHLAB::SENTRY::selftest event sent");
                    }
                    catch (Exception se)
                    {
                        Debug.LogWarning($"CRASHLAB::SENTRY::selftest error: {se.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CrashLab Sentry init error: {e.Message}");
            }
        }

        private static void ApplyEnvOverrides(SentryOptions o)
        {
            // Booleans
            if (TryGetBool("SENTRY_DEBUG", out var debug)) o.Debug = debug;
            if (TryGetBool("SENTRY_AUTO_SESSION_TRACKING", out var ast)) o.AutoSessionTracking = ast;
            // if (TryGetBool("SENTRY_CAPTURE_IN_EDITOR", out var cie)) o.CaptureInEditor = cie;
            if (TryGetBool("SENTRY_ATTACH_STACKTRACE", out var attach)) o.AttachStacktrace = attach;
            if (TryGetBool("SENTRY_SEND_DEFAULT_PII", out var pii)) o.SendDefaultPii = pii;

            // Numbers
            if (TryGetDouble("SENTRY_TRACES_SAMPLE_RATE", out var tsr)) o.TracesSampleRate = tsr;
            // ProfilesSampleRate may not be present in all SDK versions; reflection fallback provided below.
            if (TryGetDouble("SENTRY_PROFILES_SAMPLE_RATE", out var psr))
            {
                var prop = typeof(SentryOptions).GetProperty("ProfilesSampleRate");
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
                    if (!string.IsNullOrEmpty(p)) TryAddToListProperty(o, "InAppInclude", p);
                }
            }

            var inAppExcludes = Environment.GetEnvironmentVariable("SENTRY_INAPP_EXCLUDE");
            if (!string.IsNullOrWhiteSpace(inAppExcludes))
            {
                foreach (var part in inAppExcludes.Split(','))
                {
                    var p = part.Trim();
                    if (!string.IsNullOrEmpty(p)) TryAddToListProperty(o, "InAppExclude", p);
                }
            }

            // Generic overrides: any SentryUnityOptions property can be set via SENTRY_OPT_<PropertyName>
            ApplyArbitraryEnvOverrides(o);
        }

        private static void ApplyNativeDefaults(SentryOptions o) 
        {
            TrySetBool(o, "AndroidNativeSupportEnabled", true);
            TrySetBool(o, "IosNativeSupportEnabled", true);
            TrySetBool(o, "MacOsNativeSupportEnabled", true);
            TrySetBool(o, "WindowsNativeSupportEnabled", true);
            // IL2CPP line number mapping support (if available in this SDK)
            TrySetBool(o, "Il2CppLineNumberSupportEnabled", true);
            // Android ANR detection defaults (if exposed by this SDK version)
            TrySetBool(o, "AnrDetectionEnabled", true);
            TrySetInt(o, "AnrTimeout", 5000); // milliseconds, if property exists
        }

        private static void ApplyArbitraryEnvOverrides(SentryOptions o)
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

                    var prop = typeof(SentryOptions).GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
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

        private static void TrySetBool(SentryOptions o, string propName, bool value)
        {
            var p = typeof(SentryOptions).GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (p != null && p.CanWrite && p.PropertyType == typeof(bool))
            {
                try { p.SetValue(o, value); } catch { }
            }
        }

        private static void TrySetInt(SentryOptions o, string propName, int value)
        {
            var p = typeof(SentryOptions).GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (p != null && p.CanWrite && p.PropertyType == typeof(int))
            {
                try { p.SetValue(o, value); } catch { }
            }
        }

        private static void TryAddToListProperty(object o, string propName, object value)
        {
            var p = o.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (p == null) return;
            var list = p.GetValue(o) as System.Collections.IList;
            if (list == null) return;
            try { list.Add(value); } catch { }
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
            try
            {
                if (type == LogType.Exception)
                {
                    var ex = new Exception(condition);
                    if (!string.IsNullOrEmpty(stackTrace))
                    {
                        try { ex.Data["unity_stack"] = stackTrace; } catch { }
                    }
                    SentrySdk.CaptureException(ex);
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
