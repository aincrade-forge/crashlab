using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CrashLab.Editor
{
    public class TelemetryStatusWindow : EditorWindow
    {
        private const string DEFINE_SENTRY = "DIAG_SENTRY";
        private const string DEFINE_CRASHLYTICS = "DIAG_CRASHLYTICS";
        private const string DEFINE_UNITY = "DIAG_UNITY";

        [MenuItem("CrashLab/Telemetry/Status", priority = 50)]
        public static void ShowWindow()
        {
            var win = GetWindow<TelemetryStatusWindow>(false, "Telemetry Status", true);
            win.minSize = new Vector2(520, 280);
            win.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("CrashLab Telemetry Status", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Shows scripting define symbols per Build Target Group and the detected active telemetry provider.", MessageType.Info);

            var activeGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            EditorGUILayout.LabelField("Active Build Target Group:", activeGroup.ToString());
            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawHeaderRow();
                foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
                {
                    if (group == BuildTargetGroup.Unknown) continue;

                    // Some groups may be obsolete; wrap in try/catch to be safe.
                    try
                    {
                        DrawGroupRow(group, group == activeGroup);
                    }
                    catch (Exception)
                    {
                        // Ignore unsupported/obsolete groups in this Editor version.
                    }
                }
            }

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Player Settings"))
                {
                    SettingsService.OpenProjectSettings("Project/Player");
                }
                if (GUILayout.Button("Refresh"))
                {
                    Repaint();
                }
            }
        }

        private void DrawHeaderRow()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Group", EditorStyles.miniBoldLabel, GUILayout.Width(160));
                GUILayout.Label("Active Provider", EditorStyles.miniBoldLabel, GUILayout.Width(160));
                GUILayout.Label("Defines", EditorStyles.miniBoldLabel);
            }
            var rect = GUILayoutUtility.GetRect(1, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.2f));
        }

        private void DrawGroupRow(BuildTargetGroup group, bool isActive)
        {
            var defines = GetDefines(group);
            var provider = GetProvider(defines);
            using (new EditorGUILayout.HorizontalScope())
            {
                var groupLabel = isActive ? $"* {group}" : group.ToString();
                GUILayout.Label(groupLabel, GUILayout.Width(160));
                GUILayout.Label(provider, GUILayout.Width(160));
                GUILayout.Label(string.Join(";", defines));
            }
        }

        private static string GetProvider(IEnumerable<string> defines)
        {
            var hasS = false; var hasC = false; var hasU = false;
            foreach (var d in defines)
            {
                if (d == DEFINE_SENTRY) hasS = true;
                else if (d == DEFINE_CRASHLYTICS) hasC = true;
                else if (d == DEFINE_UNITY) hasU = true;
            }

            if (hasS && !hasC && !hasU) return "Sentry";
            if (!hasS && hasC && !hasU) return "Crashlytics";
            if (!hasS && !hasC && hasU) return "Unity Diagnostics";
            if (!hasS && !hasC && !hasU) return "None";
            return "Mixed"; // Multiple telemetry defines set
        }

        private static IEnumerable<string> GetDefines(BuildTargetGroup group)
        {
            var named = NamedBuildTarget.FromBuildTargetGroup(group);
            var s = PlayerSettings.GetScriptingDefineSymbols(named) ?? string.Empty;
            var parts = s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                yield return parts[i].Trim();
            }
        }
    }
}
