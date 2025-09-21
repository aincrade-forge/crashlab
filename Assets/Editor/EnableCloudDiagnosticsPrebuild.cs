// Editor/EnableCloudDiagnosticsPrebuild.cs
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class EnableCloudDiagnosticsPrebuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        // Enable Unity Cloud Diagnostics Crash Reporting only for DIAG_UNITY flavor
        var named = MapNamedBuildTarget(report.summary.platform);
        var defines = PlayerSettings.GetScriptingDefineSymbols(named) ?? string.Empty;
        var shouldEnable = HasDefine(defines, "DIAG_UNITY");
        ToggleCloudDiagnostics(shouldEnable);
        Debug.Log($"Pre-build: Unity Cloud Diagnostics Crash Reporting => {(shouldEnable ? "ENABLED" : "DISABLED")} (defines: {defines})");
    }

    private static void ToggleCloudDiagnostics(bool enabled)
    {
        // Enable Crash/Cloud Diagnostics for the build
        UnityEditor.CrashReporting.CrashReportingSettings.enabled = enabled;

        // Optional: don't capture editor exceptions
        UnityEditor.CrashReporting.CrashReportingSettings.captureEditorExceptions = enabled;

        // Optional: tweak log buffer size
        // UnityEditor.CrashReporting.CrashReportingSettings.logBufferSize = 10 * 1024;

        AssetDatabase.SaveAssets();
        // Note: runtime CrashReportHandler.enableCaptureExceptions is controlled in CrashLabTelemetry
    }

    private static bool HasDefine(string defines, string token)
    {
        return (defines ?? string.Empty)
            .Split(';')
            .Select(s => s.Trim())
            .Any(d => d == token);
    }

    private static NamedBuildTarget MapNamedBuildTarget(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return NamedBuildTarget.Android;
            case BuildTarget.iOS:
                return NamedBuildTarget.iOS;
            case BuildTarget.StandaloneOSX:
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneLinux64:
                return NamedBuildTarget.Standalone;
            default:
                try { return NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup); }
                catch { return NamedBuildTarget.Standalone; }
        }
    }
}
