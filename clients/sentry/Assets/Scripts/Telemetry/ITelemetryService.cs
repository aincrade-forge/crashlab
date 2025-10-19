using System.Collections.Generic;
using UnityEngine;

namespace CrashLab
{
    public interface ITelemetryService
    {
        void Initialize(string userId, IReadOnlyDictionary<string, string> meta, string release, string environment);
        void OnLog(string condition, string stackTrace, LogType type);
    }
}
