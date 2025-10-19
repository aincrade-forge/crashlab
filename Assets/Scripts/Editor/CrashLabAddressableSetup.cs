#if UNITY_EDITOR
using System.Linq;
using CrashLab.Actions;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace CrashLab.Editor
{
    public static class CrashLabAddressableSetup
    {
        private const string AddressablesRoot = "Assets/addressables";

        [MenuItem("CrashLab/Addressables/Sync Flood Assets")]
        public static void SyncFloodAssets()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("CrashLab Addressables: Settings asset not found.");
                return;
            }

            var labels = settings.GetLabels();
            if (!labels.Contains(AssetBundleFloodRunner.FLOOD_LABEL))
            {
                settings.AddLabel(AssetBundleFloodRunner.FLOOD_LABEL);
            }

            var group = settings.FindGroup("Default Local Group") ?? settings.DefaultGroup;
            if (group == null)
            {
                Debug.LogWarning("CrashLab Addressables: Default group missing.");
                return;
            }

            var guids = AssetDatabase.FindAssets(string.Empty, new[] { AddressablesRoot });
            if (guids.Length == 0)
            {
                Debug.LogWarning($"CrashLab Addressables: No assets found under {AddressablesRoot}.");
                return;
            }

            var registered = 0;
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, group);
                if (entry.parentGroup != group)
                {
                    settings.MoveEntry(entry, group);
                }

                entry.address = assetPath;
                entry.SetLabel(AssetBundleFloodRunner.FLOOD_LABEL, true, true);
                registered++;
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"CrashLab Addressables: Synced {registered} prefabs to label '{AssetBundleFloodRunner.FLOOD_LABEL}'.");
        }
    }
}
#endif
