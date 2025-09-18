using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class CrashLabPostBuild : IPostprocessBuildWithReport
{
    public int callbackOrder => 999; // late

    public void OnPostprocessBuild(BuildReport report)
    {
        try
        {
            var hookSw = Stopwatch.StartNew();
            var meta = ReadBuildMeta();
            if (meta == null)
            {
                UnityEngine.Debug.Log("[CrashLabPostBuild] No build metadata; skipping hooks.");
                return;
            }

            var noUpload = GetEnv("CRASHLAB_NO_UPLOAD_SYMBOLS", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
            if (noUpload)
            {
                UnityEngine.Debug.Log("[CrashLabPostBuild] CRASHLAB_NO_UPLOAD_SYMBOLS=true; skipping symbol upload.");
                return;
            }

            var flavor = meta.Flavor;
            var target = report.summary.platform;
            UnityEngine.Debug.Log($"[CrashLabPostBuild] flavor={flavor} target={target} output={meta.Output}");

            if (flavor == "sentry")
            {
                RunSentryUpload(target, meta.Output);
            }
            else if (flavor == "crashlytics")
            {
                RunCrashlyticsUpload(target, meta.Output);
            }
            hookSw.Stop();
            UnityEngine.Debug.Log($"[CrashLabPostBuild] Hooks finished in {Format(hookSw.Elapsed)}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[CrashLabPostBuild] Hook error: {e.Message}");
        }
    }

    private static void RunCrashlyticsUpload(BuildTarget target, string output)
    {
        if (target == BuildTarget.Android)
        {
            var gsp = "Assets/google-services.json";
            var sym = GuessAndroidSymbolsDir();
            if (File.Exists(gsp) && sym != null)
            {
                var sw = Stopwatch.StartNew();
                UnityEngine.Debug.Log($"[Crashlytics] Uploading Android symbols from '{sym}'...");
                ShellBash($"ANDROID=true GOOGLE_SERVICES_JSON='{gsp}' ANDROID_SYMBOLS_DIR='{sym}' ./scripts/crashlytics_upload_symbols.sh");
                sw.Stop();
                UnityEngine.Debug.Log($"[Crashlytics] Android symbol upload completed in {Format(sw.Elapsed)}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[Crashlytics] Skipping upload. Missing gsp or symbols dir. gsp={gsp} sym={sym}");
            }
        }
        else if (target == BuildTarget.iOS)
        {
            // var gsp = "Assets/GoogleService-Info.plist";
            UnityEngine.Debug.Log("[Crashlytics] iOS dSYMs are generated on archive. Run upload after Xcode archive.");
            // Optionally run: IOS=true GOOGLE_SERVICE_INFO_PLIST=gsp IOS_DSYM_DIR=path ./scripts/crashlytics_upload_symbols.sh
        }
    }

    private static void RunSentryUpload(BuildTarget target, string output)
    {
        if (target == BuildTarget.StandaloneOSX)
        {
            // Attempt to find dSYM bundles alongside the .app
            var appDir = Path.GetDirectoryName(output);
            var dsyms = Directory.Exists(appDir)
                ? Directory.GetDirectories(appDir, "*.dSYM", SearchOption.AllDirectories).FirstOrDefault()
                : null;
            if (dsyms != null && TryResolveSentryCreds(out var org, out var project, out var token, out var source))
            {
                var sw = Stopwatch.StartNew();
                var env = $"SENTRY_ORG='{EscapeBash(org)}' SENTRY_PROJECT='{EscapeBash(project)}' SENTRY_AUTH_TOKEN='{EscapeBash(token)}' ";
                UnityEngine.Debug.Log($"[Sentry] Uploading macOS symbols from '{dsyms}' (creds: {source})...");
                ShellBash(env + $"PLATFORM=macos DSYM_DIR='{dsyms}' ./scripts/sentry_upload_symbols.sh");
                sw.Stop();
                UnityEngine.Debug.Log($"[Sentry] macOS symbol upload completed in {Format(sw.Elapsed)}");
            }
            else
            {
                if (dsyms == null)
                    UnityEngine.Debug.LogWarning("[Sentry] No dSYM found near macOS app. Skipping.");
                else
                    UnityEngine.Debug.LogWarning("[Sentry] Missing Sentry credentials (org/project/auth). Configure env vars or SentryCliOptions.asset. Skipping.");
            }
        }
        else if (target == BuildTarget.Android)
        {
            var lib = GuessAndroidSymbolsDir();
            if (lib != null && TryResolveSentryCreds(out var org, out var project, out var token, out var source))
            {
                var sw = Stopwatch.StartNew();
                var env = $"SENTRY_ORG='{EscapeBash(org)}' SENTRY_PROJECT='{EscapeBash(project)}' SENTRY_AUTH_TOKEN='{EscapeBash(token)}' ";
                UnityEngine.Debug.Log($"[Sentry] Uploading Android symbols from '{lib}' (creds: {source})...");
                ShellBash(env + $"PLATFORM=android ANDROID_LIB_DIR='{lib}' ./scripts/sentry_upload_symbols.sh");
                sw.Stop();
                UnityEngine.Debug.Log($"[Sentry] Android symbol upload completed in {Format(sw.Elapsed)}");
            }
            else
            {
                if (lib == null)
                    UnityEngine.Debug.LogWarning("[Sentry] Could not guess Android symbols dir. Skipping.");
                else
                    UnityEngine.Debug.LogWarning("[Sentry] Missing Sentry credentials (org/project/auth). Configure env vars or SentryCliOptions.asset. Skipping.");
            }
        }
        else if (target == BuildTarget.iOS)
        {
            UnityEngine.Debug.Log("[Sentry] For iOS, upload dSYMs after Xcode archive using scripts/sentry_upload_symbols.sh");
        }
        else if (target == BuildTarget.StandaloneWindows64)
        {
            UnityEngine.Debug.Log("[Sentry] For Windows, set PDB_DIR and run scripts/sentry_upload_symbols.sh manually.");
        }
    }

    private static bool TryResolveSentryCreds(out string org, out string project, out string token, out string source)
    {
        org = Environment.GetEnvironmentVariable("SENTRY_ORG");
        project = Environment.GetEnvironmentVariable("SENTRY_PROJECT");
        token = Environment.GetEnvironmentVariable("SENTRY_AUTH_TOKEN");
        if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(project) && !string.IsNullOrEmpty(token))
        {
            source = "env";
            return true;
        }

        try
        {
            // Fall back to SentryCliOptions.asset (Unity Sentry package CLI config)
            var assetPath = Path.Combine("Assets", "Plugins", "Sentry", "SentryCliOptions.asset");
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (obj != null)
            {
                var so = new SerializedObject(obj);
                string Get(string name)
                    => so.FindProperty($"<{name}>k__BackingField")?.stringValue;
                var o = Get("Organization");
                var p = Get("Project");
                var t = Get("Auth");
                if (!string.IsNullOrEmpty(o) && !string.IsNullOrEmpty(p) && !string.IsNullOrEmpty(t))
                {
                    org = o; project = p; token = t; source = "SentryCliOptions.asset";
                    return true;
                }
            }
        }
        catch { /* ignore */ }

        source = null;
        return false;
    }

    private static string GuessAndroidSymbolsDir()
    {
        // Try a few known locations for IL2CPP/NDK symbols
        var candidates = new[]
        {
            Path.Combine("Library", "PlayerDataCache", "Android"),
            Path.Combine("Library", "Bee"),
            Path.Combine("Temp")
        };
        return candidates.FirstOrDefault(Directory.Exists);
    }

    private static void ShellBash(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = "-lc \"" + command.Replace("\"", "\\\"") + "\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi);
        if (p == null) throw new Exception("Failed to start bash");
        UnityEngine.Debug.Log("[CrashLabPostBuild] exec: " + command);
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        UnityEngine.Debug.Log("[CrashLabPostBuild] " + stdout);
        if (p.ExitCode != 0)
        {
            UnityEngine.Debug.LogWarning("[CrashLabPostBuild] stderr: " + stderr);
        }
    }

    private static string GetEnv(string key, string def)
        => Environment.GetEnvironmentVariable(key) ?? def;

    private static string Format(TimeSpan ts)
        => ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s"
           : ts.TotalMinutes >= 1 ? $"{(int)ts.TotalMinutes}m {ts.Seconds}s"
           : $"{ts.Seconds}s";

    private static string EscapeBash(string s) => string.IsNullOrEmpty(s) ? s : s.Replace("'", "'\"'\"'");

    private class BuildMeta
    {
        public string Target;
        public string Flavor;
        public bool Development;
        public string Output;
    }

    private static BuildMeta ReadBuildMeta()
    {
        try
        {
            var path = Path.Combine("Library", "CrashLabBuild", "build.json");
            if (!File.Exists(path)) return null;
            var text = File.ReadAllText(path);
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
            var b = new BuildMeta
            {
                Target = Read("target"),
                Flavor = Read("flavor"),
                Output = Read("output"),
                Development = text.Contains("\"development\":true")
            };
            return b;
        }
        catch
        {
            return null;
        }
    }
}
