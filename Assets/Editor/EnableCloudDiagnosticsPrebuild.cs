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
        ToggleCloudDiagnostics(true);
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
        Debug.Log("Pre-build: Services + Crash Reporting enabled.");
    }
}