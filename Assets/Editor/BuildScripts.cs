using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

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

            var (buildTarget, group) = MapTarget(target);
            ConfigureFlavor(group, flavor);
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

            var location = ResolveOutputPath(buildTarget, target, output);
            EnsureParentDir(location);

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

            Log($"Build succeeded → {location}");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            LogError(ex.ToString());
            EditorApplication.Exit(1);
        }
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

    private static void ConfigureFlavor(BuildTargetGroup group, string flavor)
    {
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group) ?? string.Empty;
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
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
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
        PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.IL2CPP);

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
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, finalAndroid);
                Log($"Android applicationId: {finalAndroid}");
                break;
            case "ios-arm64":
                var finalIos = genericId ?? iosId ?? DefaultFor("ios");
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, finalIos);
                Log($"iOS bundle id: {finalIos}");
                break;
            case "macos-arm64":
                // Optional: set standalone identifier (not strictly required)
                var finalMac = genericId ?? DefaultFor("macos");
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, finalMac);
                Log($"macOS bundle id: {finalMac}");
                break;
            case "windows-x64":
                var finalWin = genericId ?? DefaultFor("windows");
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, finalWin);
                Log($"Windows product id: {finalWin}");
                break;
        }
    }

    private static string ResolveOutputPath(BuildTarget target, string targetKey, string output)
    {
        if (!string.IsNullOrEmpty(output))
            return output;

        switch (target)
        {
            case BuildTarget.Android:
                return Path.Combine("Builds", "Android", "CrashLab.apk");
            case BuildTarget.iOS:
                return Path.Combine("Builds", "iOS"); // Xcode project dir
            case BuildTarget.StandaloneOSX:
                return Path.Combine("Builds", "macOS", "CrashLab.app");
            case BuildTarget.StandaloneWindows64:
                return Path.Combine("Builds", "Windows", "CrashLab.exe");
            default:
                throw new ArgumentOutOfRangeException(nameof(target));
        }
    }

    private static void EnsureParentDir(string path)
    {
        var fileHasExt = Path.HasExtension(path) || path.EndsWith(".app", StringComparison.OrdinalIgnoreCase);
        var dir = fileHasExt ? Path.GetDirectoryName(path) : path;
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private static string GetEnv(string key, string def) => Environment.GetEnvironmentVariable(key) ?? def;

    private static void Log(string msg) => Console.WriteLine($"[BuildScripts] {msg}");
    private static void LogError(string msg) => Console.Error.WriteLine($"[BuildScripts:ERROR] {msg}");
}
