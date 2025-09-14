using System.Collections.Generic;
using UnityEngine;

namespace CrashLab
{
    public class NoTelemetryService : ITelemetryService
    {
        public void Initialize(string userId, IReadOnlyDictionary<string, string> meta, string release, string environment)
        {
            Debug.Log($"CRASHLAB::META::{ToKvpString(meta)}");
        }

        public void OnLog(string condition, string stackTrace, LogType type)
        {
            // No-op; Unity logs go to Player.log.
        }

        private static string ToKvpString(IReadOnlyDictionary<string, string> dict)
        {
            var first = true;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var kv in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append(kv.Key).Append('=').Append(kv.Value);
            }
            return sb.ToString();
        }
    }
}

