using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CrashLab.Editor
{
    public static class TelemetryDefineSwitcher
    {
        private const string DEFINE_SENTRY = "DIAG_SENTRY";
        private const string DEFINE_CRASHLYTICS = "DIAG_CRASHLYTICS";
        private const string DEFINE_UNITY = "DIAG_UNITY";

        private static readonly string[] AllDiagDefines =
        {
            DEFINE_SENTRY,
            DEFINE_CRASHLYTICS,
            DEFINE_UNITY,
        };

        private const string MenuRoot = "CrashLab/Telemetry/";
        private const string MenuSentry = MenuRoot + "Use Sentry";
        private const string MenuCrashlytics = MenuRoot + "Use Crashlytics";
        private const string MenuUnity = MenuRoot + "Use Unity Diagnostics";
        private const string MenuNone = MenuRoot + "Use None";

        [MenuItem(MenuSentry, priority = 0)]
        public static void UseSentry() => SetActiveDefine(DEFINE_SENTRY);

        [MenuItem(MenuCrashlytics, priority = 1)]
        public static void UseCrashlytics() => SetActiveDefine(DEFINE_CRASHLYTICS);

        [MenuItem(MenuUnity, priority = 2)]
        public static void UseUnityDiagnostics() => SetActiveDefine(DEFINE_UNITY);

        [MenuItem(MenuNone, priority = 3)]
        public static void UseNone() => SetActiveDefine(null);

        [MenuItem(MenuSentry, true)]
        private static bool ValidateSentry()
        {
            Menu.SetChecked(MenuSentry, IsActive(DEFINE_SENTRY));
            return true;
        }

        [MenuItem(MenuCrashlytics, true)]
        private static bool ValidateCrashlytics()
        {
            Menu.SetChecked(MenuCrashlytics, IsActive(DEFINE_CRASHLYTICS));
            return true;
        }

        [MenuItem(MenuUnity, true)]
        private static bool ValidateUnity()
        {
            Menu.SetChecked(MenuUnity, IsActive(DEFINE_UNITY));
            return true;
        }

        [MenuItem(MenuNone, true)]
        private static bool ValidateNone()
        {
            var hasAny = IsActive(DEFINE_SENTRY) || IsActive(DEFINE_CRASHLYTICS) || IsActive(DEFINE_UNITY);
            Menu.SetChecked(MenuNone, !hasAny);
            return true;
        }

        private static void SetActiveDefine(string defineOrNull)
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (group == BuildTargetGroup.Unknown)
            {
                Debug.LogWarning("CrashLab: No active Build Target Group selected.");
                return;
            }

            var defines = new HashSet<string>(StringComparer.Ordinal);
            foreach (var d in GetDefines(group))
                if (!string.IsNullOrEmpty(d)) defines.Add(d);

            foreach (var d in AllDiagDefines)
                defines.Remove(d);

            if (!string.IsNullOrEmpty(defineOrNull))
                defines.Add(defineOrNull);

            SetDefines(group, defines);
            Debug.Log($"CrashLab: Active telemetry define set to '{defineOrNull ?? "<none>"}' for {group}.");
        }

        private static bool IsActive(string define)
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (group == BuildTargetGroup.Unknown) return false;
            foreach (var d in GetDefines(group))
                if (d == define) return true;
            return false;
        }

        private static IEnumerable<string> GetDefines(BuildTargetGroup group)
        {
            var s = PlayerSettings.GetScriptingDefineSymbolsForGroup(group) ?? string.Empty;
            var parts = s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                yield return parts[i].Trim();
            }
        }

        private static void SetDefines(BuildTargetGroup group, IEnumerable<string> defines)
        {
            var list = new List<string>();
            foreach (var d in defines)
            {
                if (!string.IsNullOrWhiteSpace(d)) list.Add(d.Trim());
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
        }
    }
}
