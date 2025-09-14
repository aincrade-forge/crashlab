using System;
using System.Collections.Generic;
using UnityEngine;

#if DIAG_CRASHLYTICS
using Firebase;
using Firebase.Extensions;
using Firebase.Crashlytics;
#endif

namespace CrashLab
{
#if DIAG_CRASHLYTICS
    public class CrashlyticsTelemetryService : ITelemetryService
    {
        public void Initialize(string userId, IReadOnlyDictionary<string, string> meta, string release, string environment)
        {
            InitializeAsync(userId, meta);
        }

        public void OnLog(string condition, string stackTrace, LogType type)
        {
            Crashlytics.Log(condition);
        }

        private static async void InitializeAsync(string userId, IReadOnlyDictionary<string, string> meta)
        {
            try
            {
                var status = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (status != DependencyStatus.Available)
                {
                    Debug.LogWarning($"Firebase dependencies not available: {status}");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Firebase CheckAndFixDependenciesAsync failed: {e.Message}");
                return;
            }

            try
            {
                Crashlytics.IsCrashlyticsCollectionEnabled = true;
                Crashlytics.SetUserId(userId);
                foreach (var kv in meta)
                {
                    Crashlytics.SetCustomKey(kv.Key, kv.Value);
                }
                Crashlytics.Log("CrashLab Crashlytics initialized");
                Debug.Log("CRASHLAB::CRASHLYTICS::initialized");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Crashlytics init failed: {e.Message}");
            }
        }
    }
#endif
}
