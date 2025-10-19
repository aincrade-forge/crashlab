using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CrashLab.Editor
{
    public static class SentryCredsTester
    {
        [MenuItem("CrashLab/Telemetry/Test Sentry Creds")]
        public static void TestMenu() => Test(false);

        [MenuItem("CrashLab/Telemetry/Test Sentry Creds (Exit)")]
        public static void TestMenuExit() => Test(true);

        // Invokable via CLI: -executeMethod CrashLab.Editor.SentryCredsTester.TestMenuExit
        public static void Test(bool exit)
        {
            var t = typeof(CrashLabPostBuild);
            var mi = t.GetMethod("TryResolveSentryCreds", BindingFlags.NonPublic | BindingFlags.Static);
            if (mi == null)
            {
                Debug.LogError("TryResolveSentryCreds not found via reflection.");
                if (exit) EditorApplication.Exit(2);
                return;
            }

            object[] args = new object[] { null, null, null, null };
            bool ok = (bool)mi.Invoke(null, args);
            var org = args[0] as string;
            var project = args[1] as string;
            var token = args[2] as string;
            var source = args[3] as string;

            var orgSafe = org ?? string.Empty;
            var projectSafe = project ?? string.Empty;
            var tokenSet = string.IsNullOrEmpty(token) ? "no" : "yes";
            var sourceSafe = source ?? string.Empty;
            Debug.Log($"[SentryCredsTester] ok={ok} org='{orgSafe}' project='{projectSafe}' token_set={tokenSet} source='{sourceSafe}'");
            if (!ok)
            {
                Debug.Log("[SentryCredsTester] Provide env SENTRY_ORG/SENTRY_PROJECT/SENTRY_AUTH_TOKEN or fill Assets/Plugins/Sentry/SentryCliOptions.asset (Organization/Project/Auth)");
            }
            if (exit) EditorApplication.Exit(ok ? 0 : 3);
        }
    }
}

