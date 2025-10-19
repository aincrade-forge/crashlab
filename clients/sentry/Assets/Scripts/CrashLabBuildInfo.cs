using UnityEngine;

namespace CrashLab
{
    // Build-time info embedded into the Player via Resources.
    // Populated by pre-build hook; read by CrashLabTelemetry at runtime.
    public class CrashLabBuildInfo : ScriptableObject
    {
        public string commitSha;
        public string branch;
        public string buildNumber;
        public string buildTimestampUtc;
    }
}

