using System;
using System.IO;
using UnityEditor;

#if DIAG_SENTRY
using Sentry;
#endif

public static class CrashLabSentryMenu
{
    [MenuItem("CrashLab/Sentry/Capture Test Message", priority = 200)]
    public static void CaptureTestMessage()
    {
#if DIAG_SENTRY
        try
        {
            SentrySdk.CaptureMessage("CrashLab test message ✨", SentryLevel.Info);
            EditorUtility.DisplayDialog("Sentry", "Captured a test message.", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Sentry", "Failed to capture message: " + e.Message, "OK");
        }
#else
        EditorUtility.DisplayDialog("Sentry", "Build with DIAG_SENTRY to use this.", "OK");
#endif
    }

    [MenuItem("CrashLab/Sentry/Upload Symbols (Last Build)", priority = 201)]
    public static void UploadSymbolsLastBuild()
    {
        try
        {
            var metaPath = Path.Combine("Library", "CrashLabBuild", "build.json");
            if (!File.Exists(metaPath))
            {
                EditorUtility.DisplayDialog("Sentry Upload", "No build metadata found.", "OK");
                return;
            }
            var text = File.ReadAllText(metaPath);
            string Read(string k)
            {
                var tag = "\"" + k + "\"";
                var i = text.IndexOf(tag, StringComparison.Ordinal);
                if (i < 0) return null;
                var c = text.IndexOf(':', i) + 1;
                var q1 = text.IndexOf('"', c) + 1;
                var q2 = text.IndexOf('"', q1);
                return text.Substring(q1, q2 - q1);
            }
            var target = Read("target");
            var output = Read("output");

            if (target == "macos-arm64")
            {
                var appDir = Path.GetDirectoryName(output);
                var dsyms = Directory.Exists(appDir) ? Directory.GetDirectories(appDir, "*.dSYM", SearchOption.AllDirectories) : Array.Empty<string>();
                if (dsyms.Length > 0)
                {
                    ShellBash($"PLATFORM=macos DSYM_DIR='{dsyms[0]}' ./scripts/sentry_upload_symbols.sh");
                    EditorUtility.DisplayDialog("Sentry Upload", "Uploaded macOS dSYM.", "OK");
                    return;
                }
                EditorUtility.DisplayDialog("Sentry Upload", "No dSYM found near app.", "OK");
            }
            else if (target == "android-arm64")
            {
                var sym = Path.Combine("Library", "PlayerDataCache", "Android");
                if (Directory.Exists(sym))
                {
                    ShellBash($"PLATFORM=android ANDROID_LIB_DIR='{sym}' ./scripts/sentry_upload_symbols.sh");
                    EditorUtility.DisplayDialog("Sentry Upload", "Uploaded Android symbols.", "OK");
                    return;
                }
                EditorUtility.DisplayDialog("Sentry Upload", "Could not locate Android symbols.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Sentry Upload", "For iOS/Windows, run scripts manually after archive/build.", "OK");
            }
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Sentry Upload", "Error: " + e.Message, "OK");
        }
    }

    private static void ShellBash(string command)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = "-lc \"" + command.Replace("\"", "\\\"") + "\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = System.Diagnostics.Process.Start(psi);
        if (p == null) throw new Exception("Failed to start bash");
        p.WaitForExit();
    }
}

