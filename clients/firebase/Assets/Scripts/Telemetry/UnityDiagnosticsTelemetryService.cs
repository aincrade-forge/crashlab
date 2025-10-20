using System;
using System.Collections.Generic;
using UnityEngine;

#if DIAG_UNITY
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.CloudDiagnostics;
#endif

namespace CrashLab
{
#if DIAG_UNITY
    public class UnityDiagnosticsTelemetryService : ITelemetryService
    {
        public void Initialize(string userId, IReadOnlyDictionary<string, string> meta, string release, string environment)
        {
            InitializeAsync(userId, meta, environment);
        }

        public void OnLog(string condition, string stackTrace, LogType type)
        {
            // No explicit breadcrumbs API; rely on logs and metadata.
        }

        public void EnsureSession(string reason = null)
        {
            _ = reason;
        }

        private static async void InitializeAsync(string userId, IReadOnlyDictionary<string, string> meta, string environment)
        {
            try
            {
                var options = new InitializationOptions().SetEnvironmentName(environment);
                await UnityServices.InitializeAsync(options);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"UnityServices.InitializeAsync failed: {e.Message}");
            }

            try
            {
                UnityEngine.CrashReportHandler.CrashReportHandler.SetUserMetadata("user_id", userId);
                foreach (var kv in meta)
                {
                    UnityEngine.CrashReportHandler.CrashReportHandler.SetUserMetadata(kv.Key, kv.Value);
                }
                Debug.Log("CRASHLAB::UNITY_DIAGNOSTICS::initialized");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CloudDiagnostics CrashReporting init failed: {e.Message}");
            }
        }
    }
#endif
}
