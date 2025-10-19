using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if DIAG_SENTRY
using Sentry;
#endif

namespace CrashLab
{
    public enum CrashLabBreadcrumbLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public static class CrashLabBreadcrumbs
    {
        private const string DefaultCategory = "crashlab";

        public static void Debug(string message, string category = DefaultCategory, IReadOnlyDictionary<string, string> data = null)
            => Add(message, CrashLabBreadcrumbLevel.Debug, category, data);

        public static void Info(string message, string category = DefaultCategory, IReadOnlyDictionary<string, string> data = null)
            => Add(message, CrashLabBreadcrumbLevel.Info, category, data);

        public static void Warning(string message, string category = DefaultCategory, IReadOnlyDictionary<string, string> data = null)
            => Add(message, CrashLabBreadcrumbLevel.Warning, category, data);

        public static void Error(string message, string category = DefaultCategory, IReadOnlyDictionary<string, string> data = null)
            => Add(message, CrashLabBreadcrumbLevel.Error, category, data);

        public static void Add(string message, CrashLabBreadcrumbLevel level, string category = DefaultCategory, IReadOnlyDictionary<string, string> data = null)
        {
#if DIAG_SENTRY
            try
            {
                var breadcrumbLevel = level switch
                {
                    CrashLabBreadcrumbLevel.Debug => BreadcrumbLevel.Debug,
                    CrashLabBreadcrumbLevel.Warning => BreadcrumbLevel.Warning,
                    CrashLabBreadcrumbLevel.Error => BreadcrumbLevel.Error,
                    _ => BreadcrumbLevel.Info
                };
                Dictionary<string, string> payload = null;
                if (data != null && data.Count > 0)
                {
                    payload = new Dictionary<string, string>(data);
                }
                SentrySdk.AddBreadcrumb(message, category: category, level: breadcrumbLevel, data: payload);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"CRASHLAB::BREADCRUMB_FAIL::{e.GetType().Name}:{e.Message}");
            }
#else
            if (data != null && data.Count > 0)
            {
                UnityEngine.Debug.Log($"CRASHLAB::BREADCRUMB::{category}::{level}::{message}::{FormatData(data)}");
            }
            else
            {
                UnityEngine.Debug.Log($"CRASHLAB::BREADCRUMB::{category}::{level}::{message}");
            }
#endif
        }

#if !DIAG_SENTRY
        private static string FormatData(IReadOnlyDictionary<string, string> data)
        {
            if (data == null || data.Count == 0) return string.Empty;
            var builder = new StringBuilder();
            var first = true;
            foreach (var kv in data)
            {
                if (!first) builder.Append(';');
                builder.Append(kv.Key).Append('=').Append(kv.Value);
                first = false;
            }
            return builder.ToString();
        }
#endif
    }
}
