using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CrashLab
{
    public static class CrashActions
    {
        private static readonly List<byte[]> _oom = new List<byte[]>();

        public static void ManagedNullRef()
        {
            object o = null;
            Debug.Log("CRASHLAB::managed_null_ref::START");
            _ = o.ToString();
        }

        public static void ManagedDivZero()
        {
            Debug.Log("CRASHLAB::managed_div_zero::START");
            var x = 1 / int.Parse("0");
            Debug.Log(x);
        }

        public static void ManagedUnhandled()
        {
            Debug.Log("CRASHLAB::managed_unhandled::START");
            throw new Exception("CrashLab: unhandled exception");
        }

        public static void ManagedUnobservedTask()
        {
            Debug.Log("CRASHLAB::managed_unobserved_task::START");
            Task.Run(() => throw new Exception("CrashLab: unobserved task exception"));
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public static void NativeAccessViolation()
        {
            Debug.Log("CRASHLAB::native_av::START");
            UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.AccessViolation);
        }

        public static void NativeAbort()
        {
            Debug.Log("CRASHLAB::native_abort::START");
            UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.Abort);
        }

        public static void NativeFatal()
        {
            Debug.Log("CRASHLAB::native_fatal::START");
            UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.FatalError);
        }

        public static void NativeStackOverflow()
        {
            Debug.Log("CRASHLAB::native_stack_overflow::START");
            RecurseForever(0);
        }

        private static void RecurseForever(int d)
        {
            RecurseForever(d + 1);
        }

        public static void AndroidAnr(int seconds = 10)
        {
            Debug.Log($"CRASHLAB::android_anr::START::{seconds}s");
#if UNITY_ANDROID
            Thread.Sleep(seconds * 1000);
#else
            Debug.Log("android_anr requested on non-Android platform");
#endif
        }

        public static void DesktopHang(int seconds = 10)
        {
            Debug.Log($"CRASHLAB::desktop_hang::START::{seconds}s");
            Thread.Sleep(seconds * 1000);
        }

        public static void OomHeap()
        {
            Debug.Log("CRASHLAB::oom_heap::START");
            try
            {
                var size = 16 * 1024 * 1024;
                while (true)
                {
                    _oom.Add(new byte[size]);
                    size = Mathf.Min(size * 2, 256 * 1024 * 1024);
                    Debug.Log($"CRASHLAB::oom_heap::ALLOC::{size}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CRASHLAB::oom_heap::EX::{ex.GetType().Name}");
            }
        }

        public static void ScheduleStartupCrash(string key = "crashlab_startup_action")
        {
            PlayerPrefs.SetString(key, "managed_unhandled");
            PlayerPrefs.Save();
            Debug.Log("CRASHLAB::startup_crash::SCHEDULED");
        }

        public static void CheckAndRunStartupCrash(string key = "crashlab_startup_action")
        {
            if (!PlayerPrefs.HasKey(key)) return;
            var action = PlayerPrefs.GetString(key, string.Empty);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            if (action == "managed_unhandled") ManagedUnhandled();
        }
    }
}

