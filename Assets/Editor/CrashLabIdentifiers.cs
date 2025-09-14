using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

public static class CrashLabIdentifiers
{
    private const string AndroidGoogleServicesPath = "Assets/google-services.json";
    private const string IosGoogleServiceInfoPath = "Assets/GoogleService-Info.plist";

    [MenuItem("CrashLab/Identifiers/Set from Google Services", priority = 10)]
    public static void SetFromGoogleServices()
    {
        int updated = 0;
        if (TrySetAndroidFromGoogleServices()) updated++;
        if (TrySetiOSFromGoogleServiceInfo()) updated++;

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "CrashLab Identifiers",
            updated > 0 ? $"Updated {updated} platform identifier(s) from Google service files." : "No identifiers were updated. Ensure your google-services files exist and contain expected keys.",
            "OK");
    }

    [MenuItem("CrashLab/Identifiers/Set Android from google-services.json", priority = 11)]
    public static void SetAndroidFromGoogleServicesMenu()
    {
        var ok = TrySetAndroidFromGoogleServices();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("CrashLab Identifiers", ok ? "Updated Android applicationId." : "Failed to update Android applicationId.", "OK");
    }

    [MenuItem("CrashLab/Identifiers/Set iOS from GoogleService-Info.plist", priority = 12)]
    public static void SetIosFromGoogleServiceInfoMenu()
    {
        var ok = TrySetIOSFromGoogleServiceInfoWrapper();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("CrashLab Identifiers", ok ? "Updated iOS bundle identifier." : "Failed to update iOS bundle identifier.", "OK");
    }

    [MenuItem("CrashLab/Identifiers/Show Current", priority = 50)]
    public static void ShowCurrent()
    {
        var android = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        var ios = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
        var standalone = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Standalone);
        EditorUtility.DisplayDialog(
            "CrashLab Identifiers",
            $"Android: {android}\n\niOS: {ios}\n\nStandalone: {standalone}",
            "OK");
    }

    private static bool TrySetAndroidFromGoogleServices()
    {
        try
        {
            if (!File.Exists(AndroidGoogleServicesPath))
                return false;
            var text = File.ReadAllText(AndroidGoogleServicesPath);
            // Look for: "package_name": "com.example.app"
            var m = Regex.Match(text, "\\\"package_name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
            if (!m.Success)
                return false;
            var pkg = m.Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(pkg))
                return false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, pkg);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TrySetIOSFromGoogleServiceInfoWrapper()
    {
        return TrySetiOSFromGoogleServiceInfo();
    }

    private static bool TrySetiOSFromGoogleServiceInfo()
    {
        try
        {
            if (!File.Exists(IosGoogleServiceInfoPath))
                return false;
            var text = File.ReadAllText(IosGoogleServiceInfoPath);
            // Try GoogleService-Info.plist key BUNDLE_ID first
            var m = Regex.Match(text, @"<key>\s*BUNDLE_ID\s*</key>\s*<string>([^<]+)</string>");
            if (!m.Success)
            {
                // Fallback: generic plist key CFBundleIdentifier (rarely present in this file)
                m = Regex.Match(text, @"<key>\s*CFBundleIdentifier\s*</key>\s*<string>([^<]+)</string>");
            }
            if (!m.Success)
                return false;
            var bundleId = m.Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(bundleId))
                return false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, bundleId);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

