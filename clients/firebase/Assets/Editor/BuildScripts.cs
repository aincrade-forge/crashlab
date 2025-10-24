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
    
    private static void ConfigureIl2Cpp(BuildTargetGroup group, BuildTarget target, string targetKey)
    {
        var named = GetNamedBuildTarget(group, target);
        PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.IL2CPP);

        if (target == BuildTarget.Android)
        {
#if UNITY_ANDROID
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
}
