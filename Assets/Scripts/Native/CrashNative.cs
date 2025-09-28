using System;
using System.Runtime.InteropServices;

namespace CrashLab
{
    internal static class CrashNative
    {
#if (UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR_WIN
        [DllImport("libc")]
        private static extern int raise(int sig);
        private const int SIGILL = 4;
        private const int SIGABRT = 6;
        private const int SIGSEGV = 11;
#endif

        // Terminate process via abort signal
        public static void Abort()
        {
            try
            {
#if (UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR_WIN
                raise(SIGABRT);
                // If returning, ensure termination
                Environment.FailFast("CrashLab Abort fallback");
#else
                Environment.FailFast("CrashLab Abort");
#endif
            }
            catch
            {
                Environment.FailFast("CrashLab Abort (exception)");
            }
        }

        // Trigger a segmentation fault signal
        public static void Segv()
        {
            try
            {
#if (UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR_WIN
                raise(SIGSEGV);
                Environment.FailFast("CrashLab Segv fallback");
#else
                // Best-effort: write to null; if not fatal, force terminate
                try { Marshal.WriteInt32(IntPtr.Zero, 0); }
                catch { /* ignore */ }
                Environment.FailFast("CrashLab Segv");
#endif
            }
            catch
            {
                Environment.FailFast("CrashLab Segv (exception)");
            }
        }

        // Trigger an illegal instruction signal
        public static void IllegalInstruction()
        {
            try
            {
#if (UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR_WIN
                raise(SIGILL);
                Environment.FailFast("CrashLab IllegalInstruction fallback");
#else
                Environment.FailFast("CrashLab IllegalInstruction");
#endif
            }
            catch
            {
                Environment.FailFast("CrashLab IllegalInstruction (exception)");
            }
        }

    }
}
