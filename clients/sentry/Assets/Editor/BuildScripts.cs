using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools.TestRunner.Api;
using System.Diagnostics;

    public static class BuildScripts
    {
        // Entry point for CLI: -executeMethod BuildScripts.BuildRelease
        public static void BuildRelease()
        {
            try
            {
                var target = GetEnv("TARGET", "macos-arm64");
                var flavor = GetEnv("FLAVOR", "unity"); // sentry | crashlytics | unity
                var output = GetEnv("OUTPUT", string.Empty);
                var development = GetEnv("DEV_MODE", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
                var path = BuildOnce(target, flavor, development, output);
                Log($"Build succeeded → {path}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                EditorApplication.Exit(1);
            }
        }

        // Entry point for CLI: -executeMethod BuildScripts.BuildMatrix
        // Optional env: TARGETS="windows-x64,macos-arm64,android-arm64,ios-arm64"
        //                FLAVORS="sentry,unity,crashlytics"  DEV_MODE=true/false
        public static void BuildMatrix()
        {
            try
            {
                var matrixSw = Stopwatch.StartNew();
                var targets = (GetEnv("TARGETS", "windows-x64,macos-arm64,android-arm64,ios-arm64")
                    .Split(',')).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                var dev = GetEnv("DEV_MODE", "false").Equals("true", StringComparison.OrdinalIgnoreCase);

                // Plan tasks
                var tasks = new System.Collections.Generic.List<(string target, string flavor)>();
                foreach (var t in targets)
                    foreach (var f in FlavorsForTarget(t))
                        tasks.Add((t, f));

                var total = tasks.Count;
                Log($"=== Matrix plan: {total} builds ===");
                for (int i = 0; i < tasks.Count; i++)
                {
                    var (t, f) = tasks[i];
                    Log($"[{i + 1}/{total}] Start {t}/{f} (dev={dev})");
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        var path = BuildOnce(t, f, dev);
                        sw.Stop();
                        // ETA calculation
                        var avg = sw.Elapsed; // default for first item
                        if (i > 0)
                        {
                            var done = i; // already finished before this one
                            var elapsedSoFar = matrixSw.Elapsed;
                            avg = TimeSpan.FromMilliseconds(elapsedSoFar.TotalMilliseconds / (done + 1));
                        }
                        var remaining = total - (i + 1);
                        var eta = TimeSpan.FromMilliseconds(avg.TotalMilliseconds * remaining);
                        Log($"[{i + 1}/{total}] Done {t}/{f} → {path} (took {Format(sw.Elapsed)}, ETA {Format(eta)})");
                        Log("    • Uploading symbols runs post-build; watch [CrashLabPostBuild] logs.");
                    }
                    catch (Exception ex)
                    {
                        LogError($"[{i + 1}/{total}] FAIL {t}/{f}: {ex.Message}");
                        throw; // stop matrix on first failure
                    }
                }
                matrixSw.Stop();
                Log($"=== Matrix completed in {Format(matrixSw.Elapsed)} ===");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                EditorApplication.Exit(1);
            }
        }

        // Non-exiting build API for Editor menu usage
        public static string BuildOnce(string target, string flavor, bool development, string output = "")
        {
            var (buildTarget, group) = MapTarget(target);
            ConfigureFlavor(group, buildTarget, flavor);
            ConfigureIdentifiers(group, target, flavor);
            ConfigureIl2Cpp(group, buildTarget, target);

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                // Fallback to a known scene path
                if (File.Exists("Assets/Scenes/SampleScene.unity"))
                    scenes = new[] { "Assets/Scenes/SampleScene.unity" };
                else
                    throw new Exception("No scenes found to build. Enable at least one scene in Build Settings.");
            }

            var artifactDir = ResolveArtifactDir(target, flavor);
            EnsureDir(artifactDir);
            var location = ResolveOutputPath(buildTarget, target, flavor, output, artifactDir);
            EnsureParentDir(location);
            WriteBuildMetadata(target, flavor, development, location, artifactDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = buildTarget,
                locationPathName = location,
                options = development ? BuildOptions.Development : BuildOptions.None
            };

            var sw = Stopwatch.StartNew();
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed: {report.summary.result} - {report.summary.totalErrors} errors");
            }
            sw.Stop();
            Log($"Build finished for {target}/{flavor} (elapsed {Format(sw.Elapsed)})");

            return location;
        }

    private static (BuildTarget, BuildTargetGroup) MapTarget(string target)
    {
        switch (target)
        {
            case "android-arm64":
                return (BuildTarget.Android, BuildTargetGroup.Android);
            case "ios-arm64":
                return (BuildTarget.iOS, BuildTargetGroup.iOS);
            case "macos-arm64":
                return (BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone);
            case "windows-x64":
                return (BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone);
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported TARGET value");
        }
    }

    // Define telemetry flavors supported per target platform
    private static string[] FlavorsForTarget(string target)
    {
        switch (target)
        {
            case "windows-x64":
                return new[] { "sentry", "unity" }; // Crashlytics not supported on Windows
            case "macos-arm64":
                return new[] { "sentry", "unity" }; // Crashlytics not supported on desktop macOS
            case "android-arm64":
                return new[] { "sentry", "crashlytics", "unity" };
            case "ios-arm64":
                return new[] { "sentry", "crashlytics", "unity" };
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported TARGET value");
        }
    }

    private static void ConfigureFlavor(BuildTargetGroup group, BuildTarget buildTarget, string flavor)
    {
        var named = GetNamedBuildTarget(group, buildTarget);
        var defines = PlayerSettings.GetScriptingDefineSymbols(named) ?? string.Empty;
        string[] clear = { "DIAG_SENTRY", "DIAG_CRASHLYTICS", "DIAG_UNITY" };
        foreach (var c in clear)
            defines = RemoveDefine(defines, c);

        var add = flavor.ToLowerInvariant() switch
        {
            "sentry" => "DIAG_SENTRY",
            "crashlytics" => "DIAG_CRASHLYTICS",
            _ => "DIAG_UNITY",
        };
        defines = AddDefine(defines, add);
        PlayerSettings.SetScriptingDefineSymbols(named, defines);
        Log($"Flavor set: {flavor} → define {add}");

        // Toggle Unity Cloud Diagnostics Crash Reporting based on flavor
        // Enabled only for DIAG_UNITY builds; disabled for Sentry/Crashlytics flavors to avoid duplicates
        var enableCloudCrash = add == "DIAG_UNITY";
        try
        {
            SetUnityCloudCrashReporting(enableCloudCrash);
            Log($"Unity Cloud Diagnostics Crash Reporting: {(enableCloudCrash ? "enabled" : "disabled")} for flavor={flavor}");
        }
        catch (Exception e)
        {
            LogError($"Failed to update Unity Connect CrashReporting settings: {e.Message}");
        }

        // SentryOptions.asset is now managed by a PreBuild hook (SentryPreBuildHook).
        // No action needed here to avoid duplication.
    }

    private static string AddDefine(string defines, string add)
    {
        var parts = defines.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (!parts.Contains(add)) parts.Add(add);
        return string.Join(";", parts);
    }

    private static string RemoveDefine(string defines, string rem)
    {
        var parts = defines.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s) && s != rem).ToList();
        return string.Join(";", parts);
    }

    private static void ConfigureIl2Cpp(BuildTargetGroup group, BuildTarget target, string targetKey)
    {
        var named = GetNamedBuildTarget(group, target);
        PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.IL2CPP);

        if (target == BuildTarget.Android)
        {
#if UNITY_ANDROID
            // Prefer APK by default
            EditorUserBuildSettings.buildAppBundle = false;
            // ARM64 only
            UnityEditor.PlayerSettings.Android.targetArchitectures = UnityEditor.AndroidArchitecture.ARM64;
#endif
        }

        if (target == BuildTarget.StandaloneOSX)
        {
#if UNITY_EDITOR_OSX
            // Let default be ARM64 on Apple Silicon; Unity 2021+ can build universal via settings.
            // No explicit API needed here for IL2CPP; ensure backend set above.
#endif
        }

        if (target == BuildTarget.StandaloneWindows64)
        {
            // Note: Building Windows IL2CPP may require a Windows Editor/Toolchain.
        }
    }

    private static NamedBuildTarget GetNamedBuildTarget(BuildTargetGroup group, BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return NamedBuildTarget.Android;
            case BuildTarget.iOS:
                return NamedBuildTarget.iOS;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneOSX:
            case BuildTarget.StandaloneLinux64:
                return NamedBuildTarget.Standalone;
            default:
                // Fallback to mapping by group when possible
                try { return NamedBuildTarget.FromBuildTargetGroup(group); }
                catch { return NamedBuildTarget.Standalone; }
        }
    }

    private static void ConfigureIdentifiers(BuildTargetGroup group, string targetKey, string flavor)
    {
        // No-op for current setup: keep application/bundle identifiers as configured
        // in ProjectSettings or via external build tooling. This avoids mismatches
        // with service configuration files (e.g., google-services.json).
        Log("Skipping application identifier modification");
    }

    private static string ResolveOutputPath(BuildTarget target, string targetKey, string flavor, string output, string artifactDir)
    {
        if (!string.IsNullOrEmpty(output))
            return output;

        switch (target)
        {
            case BuildTarget.Android:
                return Path.Combine(artifactDir, "CrashLab.apk");
            case BuildTarget.iOS:
                return artifactDir; // Xcode project dir
            case BuildTarget.StandaloneOSX:
                return Path.Combine(artifactDir, "CrashLab.app");
            case BuildTarget.StandaloneWindows64:
                return Path.Combine(artifactDir, "CrashLab.exe");
            default:
                throw new ArgumentOutOfRangeException(nameof(target));
        }
    }

    private static string ResolveArtifactDir(string target, string flavor)
    {
        var root = Environment.GetEnvironmentVariable("ARTIFACTS_ROOT");
        if (!string.IsNullOrEmpty(root))
        {
            return Path.Combine(root, $"{target}-{flavor}");
        }

        return Path.Combine("Artifacts", $"{target}-{flavor}");
    }

    private static void EnsureParentDir(string path)
    {
        var fileHasExt = Path.HasExtension(path) || path.EndsWith(".app", StringComparison.OrdinalIgnoreCase);
        var dir = fileHasExt ? Path.GetDirectoryName(path) : path;
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private static void EnsureDir(string dir)
    {
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    private static void WriteBuildMetadata(string target, string flavor, bool development, string output, string artifactDir)
    {
        try
        {
            var dir = Path.Combine("Library", "CrashLabBuild");
            Directory.CreateDirectory(dir);
            var commit = GetGitCommitShort() ?? Environment.GetEnvironmentVariable("COMMIT_SHA") ?? "";
            var json = "{" +
                       $"\"target\":\"{target}\"," +
                       $"\"flavor\":\"{flavor}\"," +
                       $"\"development\":{(development ? "true" : "false")}," +
                       $"\"output\":\"{EscapeJson(output)}\"," +
                       $"\"commit_sha\":\"{EscapeJson(commit)}\"" +
                       "}";
            File.WriteAllText(Path.Combine(dir, "build.json"), json);
            // Also write into artifact directory for convenience
            EnsureDir(artifactDir);
            File.WriteAllText(Path.Combine(artifactDir, "build.json"), json);
        }
        catch (Exception e)
        {
            LogError($"Failed to write build metadata: {e.Message}");
        }
    }

    // ----- Test Matrix -----

    // CLI entry: -executeMethod BuildScripts.TestMatrix
    // Runs EditMode tests per flavor for the active Editor platform.
    // Env: FLAVORS to limit set (default: per current platform), STOP_ON_FAIL=true (default true)
    public static void TestMatrix()
    {
        try
        {
            var current = EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 ? "windows-x64"
                        : EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX ? "macos-arm64"
                        : EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? "android-arm64"
                        : EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ? "ios-arm64"
                        : "macos-arm64";

            var stopOnFail = !GetEnv("STOP_ON_FAIL", "true").Equals("false", StringComparison.OrdinalIgnoreCase);
            var flavorsEnv = GetEnv("FLAVORS", null);
            var flavors = !string.IsNullOrEmpty(flavorsEnv)
                ? flavorsEnv.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray()
                : FlavorsForTarget(current);

            foreach (var f in flavors)
            {
                try
                {
                    RunEditModeTestsForFlavor(f);
                }
                catch (Exception ex)
                {
                    LogError($"Tests failed for flavor={f}: {ex.Message}");
                    if (stopOnFail) throw;
                }
            }

            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            LogError(ex.ToString());
            EditorApplication.Exit(1);
        }
    }

    private static void RunEditModeTestsForFlavor(string flavor)
    {
        // Switch defines via same flavor config used in builds
        var (_, group) = MapTarget(GuessTargetKeyFromActive());
        ConfigureFlavor(group, EditorUserBuildSettings.activeBuildTarget, flavor);

        var api = new TestRunnerApi();
        var filter = new Filter()
        {
            testMode = TestMode.EditMode
        };

        var done = false; bool success = false;
        api.RegisterCallbacks(new TestCallbacks(r => { success = r; done = true; }));
        api.Execute(new ExecutionSettings(filter));

        // Busy wait until callback signals completion (Editor-only context)
        var start = DateTime.UtcNow;
        while (!done)
        {
            if ((DateTime.UtcNow - start).TotalMinutes > 10)
            {
                throw new TimeoutException("EditMode tests timed out");
            }
            System.Threading.Thread.Sleep(100);
        }

        if (!success)
            throw new Exception("EditMode tests failed");
        Log($"EditMode tests passed for flavor={flavor}");
    }

    private class TestCallbacks : ICallbacks
    {
        private readonly Action<bool> _done;
        private bool _ok = true;
        public TestCallbacks(Action<bool> done) { _done = done; }
        public void RunStarted(ITestAdaptor testsToRun) {}
        public void RunFinished(ITestResultAdaptor result)
        {
            _ok = result.FailCount == 0 && result.InconclusiveCount == 0 && result.SkipCount == 0;
            _done?.Invoke(_ok);
        }
        public void TestStarted(ITestAdaptor test) {}
        public void TestFinished(ITestResultAdaptor result) {}
    }

    private static string GuessTargetKeyFromActive()
        => EditorUserBuildSettings.activeBuildTarget switch
        {
            BuildTarget.Android => "android-arm64",
            BuildTarget.iOS => "ios-arm64",
            BuildTarget.StandaloneWindows64 => "windows-x64",
            BuildTarget.StandaloneOSX => "macos-arm64",
            _ => "macos-arm64"
        };

    private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string GetEnv(string key, string def) => Environment.GetEnvironmentVariable(key) ?? def;

    private static void Log(string msg) => Console.WriteLine($"[BuildScripts] {msg}");
    private static void LogError(string msg) => Console.Error.WriteLine($"[BuildScripts:ERROR] {msg}");

    private static string GetGitCommitShort()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-lc 'git rev-parse --short=9 HEAD'",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p == null) return null;
            var outp = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit();
            return string.IsNullOrEmpty(outp) ? null : outp;
        }
        catch { return null; }
    }

    private static string Format(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }

    // --- Unity Cloud Diagnostics CrashReporting toggle ---
    private static void SetUnityCloudCrashReporting(bool enabled)
    {
        var path = Path.Combine("ProjectSettings", "UnityConnectSettings.asset");
        if (!File.Exists(path)) throw new FileNotFoundException(path);
        var text = File.ReadAllText(path);

        // Replace the specific CrashReportingSettings m_Enabled line
        // We search for the marker block then replace the next m_Enabled: value within that block
        const string blockStart = "CrashReportingSettings:";
        var idx = text.IndexOf(blockStart, StringComparison.Ordinal);
        if (idx < 0) throw new Exception("CrashReportingSettings block not found");
        var blockEnd = text.IndexOf("UnityPurchasingSettings:", idx, StringComparison.Ordinal);
        if (blockEnd < 0) blockEnd = text.Length;

        var block = text.Substring(idx, blockEnd - idx);
        var newBlock = System.Text.RegularExpressions.Regex.Replace(
            block,
            @"(^\s*m_Enabled:\s*)([01])\s*$",
            m => m.Groups[1].Value + (enabled ? "1" : "0"),
            System.Text.RegularExpressions.RegexOptions.Multiline);

        if (block == newBlock) return; // no change
        var newText = text.Substring(0, idx) + newBlock + text.Substring(blockEnd);
        File.WriteAllText(path, newText);
    }

}
