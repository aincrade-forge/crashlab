using System.Collections.Generic;
using System.Text;

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
            if (data != null && data.Count > 0)
            {
                UnityEngine.Debug.Log($"CRASHLAB::BREADCRUMB::{category}::{level}::{message}::{FormatData(data)}");
            }
            else
            {
                UnityEngine.Debug.Log($"CRASHLAB::BREADCRUMB::{category}::{level}::{message}");
            }
        }

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
    }
}
