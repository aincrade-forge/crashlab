using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrashLab.Editor
{
    // Single pre-build entry point for all CrashLab prep steps so it works
    // for any build path (Build Settings UI, CLI, or custom menu scripts).
    public class CrashLabPreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                var group = BuildPipeline.GetBuildTargetGroup(report.summary.platform);
                var named = NamedBuildTarget.FromBuildTargetGroup(group);
                var defines = (PlayerSettings.GetScriptingDefineSymbols(named) ?? string.Empty)
                    .Split(';')
                    .Select(d => d.Trim())
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToArray();

                bool isSentry = defines.Contains("DIAG_SENTRY");
                bool isUnityDiag = defines.Contains("DIAG_UNITY");
                // bool isCrashlytics = defines.Contains("DIAG_CRASHLYTICS"); // reserved

                UpdateSentryOptionsAsset(isSentry);
                SetUnityCloudCrashReporting(isUnityDiag);

                Debug.Log($"[CrashLab] PreBuild: sentry={isSentry} unity_diag={isUnityDiag} target={report.summary.platform}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CrashLab] PreBuild error: {e.Message}");
            }
        }

        private static void UpdateSentryOptionsAsset(bool enable)
        {
            var assetPath = Path.Combine("Assets", "Resources", "Sentry", "SentryOptions.asset");
            var optionsObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (optionsObj == null)
            {
                // Sentry package or asset not present; nothing to do.
                return;
            }

            var so = new SerializedObject(optionsObj);
            bool changed = false;
            void SetBool(string propName, bool value)
            {
                var p = so.FindProperty(propName);
                if (p != null && p.boolValue != value)
                {
                    p.boolValue = value;
                    changed = true;
                }
            }

            SetBool("<Enabled>k__BackingField", enable);
            if (enable)
            {
                // Ensure native integrations are on so native crashes are captured in Players
                SetBool("<AndroidNativeSupportEnabled>k__BackingField", true);
                SetBool("<IosNativeSupportEnabled>k__BackingField", true);
                SetBool("<MacosNativeSupportEnabled>k__BackingField", true);
                SetBool("<WindowsNativeSupportEnabled>k__BackingField", true);
                SetBool("<Il2CppLineNumberSupportEnabled>k__BackingField", true);
                SetBool("<Debug>k__BackingField", true); // helpful diagnostics
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(optionsObj);
                AssetDatabase.SaveAssets();
            }
        }

        // Toggle Unity Cloud Diagnostics Crash Reporting (Project Settings).
        private static void SetUnityCloudCrashReporting(bool enabled)
        {
            var path = Path.Combine("ProjectSettings", "UnityConnectSettings.asset");
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            var text = File.ReadAllText(path);

            const string blockStart = "CrashReportingSettings:";
            var idx = text.IndexOf(blockStart, StringComparison.Ordinal);
            if (idx < 0) throw new Exception("CrashReportingSettings block not found");

            var candidates = new[] { "UnityPurchasingSettings:", "UnityAnalyticsSettings:", "UnityAdsSettings:", "PerformanceReportingSettings:" };
            var ends = candidates.Select(c => text.IndexOf(c, idx, StringComparison.Ordinal)).Where(i => i > idx);
            var blockEnd = ends.Any() ? ends.Min() : text.Length;

            var block = text.Substring(idx, blockEnd - idx);
            var newBlock = Regex.Replace(
                block,
                @"(^\s*m_Enabled:\s*)([01])\s*$",
                m => m.Groups[1].Value + (enabled ? "1" : "0"),
                RegexOptions.Multiline);

            if (block == newBlock) return; // no change
            var newText = text.Substring(0, idx) + newBlock + text.Substring(blockEnd);
            File.WriteAllText(path, newText);
        }
    }
}

