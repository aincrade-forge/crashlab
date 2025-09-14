using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools.TestRunner.Api;

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
                var targets = (GetEnv("TARGETS", "windows-x64,macos-arm64,android-arm64,ios-arm64")
                    .Split(',')).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                var dev = GetEnv("DEV_MODE", "false").Equals("true", StringComparison.OrdinalIgnoreCase);

                foreach (var t in targets)
                {
                    foreach (var flavor in FlavorsForTarget(t))
                    {
                        Log($"=== Building {t} / {flavor} (dev={dev}) ===");
                        try
                        {
                            var path = BuildOnce(t, flavor, dev);
                            Log($"OK: {t}/{flavor} → {path}");
                        }
                        catch (Exception ex)
                        {
                            LogError($"FAIL: {t}/{flavor}: {ex.Message}");
                            throw; // stop matrix on first failure
                        }
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

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed: {report.summary.result} - {report.summary.totalErrors} errors");
            }

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
        // Allow explicit IDs via env vars; fall back to sensible defaults per flavor
        // Generic override
        var genericId = GetEnv("BUNDLE_ID", null);

        // Platform-specific overrides
        var androidId = GetEnv("BUNDLE_ID_ANDROID", GetEnv("ANDROID_APPLICATION_ID", null));
        var iosId = GetEnv("BUNDLE_ID_IOS", GetEnv("IOS_BUNDLE_ID", null));

        // Default patterns if nothing provided
        string DefaultFor(string platform)
            => $"com.aincrade.crashlab.{flavor}.{platform}".ToLowerInvariant();

        switch (targetKey)
        {
            case "android-arm64":
                var finalAndroid = genericId ?? androidId ?? DefaultFor("android");
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, finalAndroid);
                Log($"Android applicationId: {finalAndroid}");
                break;
            case "ios-arm64":
                var finalIos = genericId ?? iosId ?? DefaultFor("ios");
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, finalIos);
                Log($"iOS bundle id: {finalIos}");
                break;
            case "macos-arm64":
                // Optional: set standalone identifier (not strictly required)
                var finalMac = genericId ?? DefaultFor("macos");
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, finalMac);
                Log($"macOS bundle id: {finalMac}");
                break;
            case "windows-x64":
                var finalWin = genericId ?? DefaultFor("windows");
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, finalWin);
                Log($"Windows product id: {finalWin}");
                break;
        }
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
        => Path.Combine("Artifacts", $"{target}-{flavor}");

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
            var json = "{" +
                       $"\"target\":\"{target}\"," +
                       $"\"flavor\":\"{flavor}\"," +
                       $"\"development\":{(development ? "true" : "false")}," +
                       $"\"output\":\"{EscapeJson(output)}\"" +
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
}
