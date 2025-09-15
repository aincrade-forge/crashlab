using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrashLab.Editor
{
    // Ensures SentryOptions.asset matches the active telemetry flavor for any build path
    // (Build Settings window, CLI, or custom menu).
    public class SentryPreBuildHook : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                var group = BuildPipeline.GetBuildTargetGroup(report.summary.platform);
                var named = NamedBuildTarget.FromBuildTargetGroup(group);
                var defines = PlayerSettings.GetScriptingDefineSymbols(named) ?? string.Empty;
                bool useSentry = defines.Split(';').Any(d => string.Equals(d.Trim(), "DIAG_SENTRY", StringComparison.Ordinal));
                UpdateSentryOptionsAsset(useSentry);
                UnityEngine.Debug.Log($"[CrashLab] PreBuild: SentryOptions Enabled={useSentry} for target={report.summary.platform}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[CrashLab] PreBuild hook failed to update SentryOptions: {e.Message}");
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
    }
}

