using UnityEditor;

public static class CrashLabBuildMenu
{
    [MenuItem("CrashLab/Build/Android • Crashlytics (APK)", priority = 100)]
    public static void BuildAndroidCrashlytics()
    {
        RunBuild("android-arm64", "crashlytics", false);
    }

    [MenuItem("CrashLab/Build/iOS • Crashlytics (Xcode)", priority = 101)]
    public static void BuildIOSCrashlytics()
    {
        RunBuild("ios-arm64", "crashlytics", false);
    }

    [MenuItem("CrashLab/Build/Android • Sentry (APK)", priority = 102)]
    public static void BuildAndroidSentry()
    {
        RunBuild("android-arm64", "sentry", false);
    }

    [MenuItem("CrashLab/Build/iOS • Sentry (Xcode)", priority = 103)]
    public static void BuildIOSSentry()
    {
        RunBuild("ios-arm64", "sentry", false);
    }

    [MenuItem("CrashLab/Build/macOS • Unity (App)", priority = 110)]
    public static void BuildMacUnity()
    {
        RunBuild("macos-arm64", "unity", false);
    }

    [MenuItem("CrashLab/Build/macOS • Unity (Dev)", priority = 111)]
    public static void BuildMacUnityDev()
    {
        RunBuild("macos-arm64", "unity", true);
    }

    [MenuItem("CrashLab/Build/Windows • Unity (Exe)", priority = 120)]
    public static void BuildWindowsUnity()
    {
        RunBuild("windows-x64", "unity", false);
    }

    private static void RunBuild(string target, string flavor, bool dev)
    {
        try
        {
            var path = BuildScripts.BuildOnce(target, flavor, dev);
            EditorUtility.DisplayDialog("CrashLab Build", $"Build succeeded:\n{path}", "OK");
            EditorUtility.RevealInFinder(path);
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("CrashLab Build", $"Build failed:\n{ex.Message}", "OK");
            throw;
        }
    }
}
