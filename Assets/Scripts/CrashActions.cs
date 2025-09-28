using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace CrashLab
{
    public static class CrashActions
    {
        private static readonly List<byte[]> _oom = new List<byte[]>();
        private static readonly int _divZero = 0;

        public static void ManagedNullRef()
        {
            object o = null;
            Debug.Log("CRASHLAB::managed_null_ref::START");
            _ = o.ToString();
        }

        public static void ManagedDivZero()
        {
            Debug.Log("CRASHLAB::managed_div_zero::START");
            throw new DivideByZeroException("CrashLab: managed div zero");
        }

        public static void ManagedUnhandled()
        {
            Debug.Log("CRASHLAB::managed_unhandled::START");
            throw new Exception("CrashLab: unhandled exception");
        }

        public static void ManagedIndexOutOfRange()
        {
            Debug.Log("CRASHLAB::managed_index_out_of_range::START");
            var arr = new int[1];
            var x = arr[2];
            Debug.Log(x);
        }

        public static void ManagedKeyNotFound()
        {
            Debug.Log("CRASHLAB::managed_key_not_found::START");
            var dict = new Dictionary<string, int>();
            var v = dict["missing"]; // KeyNotFoundException
            Debug.Log(v);
        }

        public static void ManagedInvalidOperation_ModifiedDuringEnumeration()
        {
            Debug.Log("CRASHLAB::managed_invalid_operation::START");
            var list = new List<int> { 1, 2, 3 };
            foreach (var _ in list)
            {
                list.Add(4); // InvalidOperationException
            }
        }

        public static void ManagedAggregate()
        {
            Debug.Log("CRASHLAB::managed_aggregate::START");
            try
            {
                var t1 = Task.Run(() => throw new InvalidOperationException("t1"));
                var t2 = Task.Run(() => throw new ArgumentException("t2"));
                Task.WaitAll(t1, t2);
            }
            catch (AggregateException ae)
            {
                // Re-throw as unhandled to crash
                throw ae.Flatten();
            }
        }

        public static void ManagedUnobservedTask()
        {
            Debug.Log("CRASHLAB::managed_unobserved_task::START");
            Task.Run(() => throw new Exception("CrashLab: unobserved task exception"));
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public static void BackgroundThreadUnhandled()
        {
            Debug.Log("CRASHLAB::background_thread_unhandled::START");
            var thread = new Thread(() => throw new Exception("CrashLab: background thread exception"));
            thread.IsBackground = true;
            thread.Start();
        }

        public static void ThreadPoolUnhandled()
        {
            Debug.Log("CRASHLAB::threadpool_unhandled::START");
            ThreadPool.QueueUserWorkItem(_ => throw new Exception("CrashLab: threadpool unhandled exception"));
        }

        public static void UnityApiFromWorker()
        {
            Debug.Log("CRASHLAB::unity_api_from_worker::START");
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // Accessing Unity APIs off main thread is unsafe; expect errors/logs, not guaranteed crash
                    var cam = Camera.main; // may be null or trigger warning
                    Debug.Log($"Worker accessed Camera.main: {(cam ? cam.name : "<null>")}");
                    Resources.Load("NonExistent");
                }
                catch (Exception e)
                {
                    // Explicitly capture so it shows up even if the runtime swallows
                    // background thread exceptions.
#if DIAG_SENTRY
                    try { Sentry.SentrySdk.CaptureException(e); } catch { }
#endif
                    Debug.LogException(e);
                }
            });
        }

        public static void NativeAccessViolation()
        {
            Debug.Log("CRASHLAB::native_av::START");
            CrashNative.Segv();
        }

        public static void NativeAbort()
        {
            Debug.Log("CRASHLAB::native_abort::START");
            CrashNative.Abort();
        }

        public static void NativeFatal()
        {
            Debug.Log("CRASHLAB::native_fatal::START");
            try
            {
                Utils.ForceCrash(ForcedCrashCategory.FatalError);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CRASHLAB::native_fatal::FALLBACK::{ex.GetType().Name}:{ex.Message}");
                CrashNative.Abort();
            }
        }

        public static void NativeStackOverflow()
        {
            Debug.Log("CRASHLAB::native_stack_overflow::START");
            try
            {
                Utils.ForceCrash(ForcedCrashCategory.StackOverflow);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CRASHLAB::native_stack_overflow::FALLBACK::{ex.GetType().Name}:{ex.Message}");
                CrashNative.Abort();
            }
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

        public static void SyncWaitHang(int seconds = 10)
        {
            Debug.Log($"CRASHLAB::sync_wait_hang::START::{seconds}s");
            try
            {
                Task.Delay(TimeSpan.FromSeconds(seconds)).Wait();
            }
            catch (Exception)
            {
                // ignore
            }
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

        public static void FileWriteDenied()
        {
            Debug.Log("CRASHLAB::file_write_denied::START");
            try
            {
#if UNITY_STANDALONE || UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID
                var path = System.IO.Path.DirectorySeparatorChar == '\\' ? "C:/Windows/System32/_crashlab_denied.txt" : "/_crashlab_denied.txt";
                System.IO.File.WriteAllText(path, "test");
#else
                System.IO.File.WriteAllText("/_crashlab_denied.txt", "test");
#endif
            }
            catch (Exception e)
            {
                // Re-throw to ensure it's captured as crash (UnauthorizedAccessException/IOException)
                throw e;
            }
        }

        public static void JsonParseError()
        {
            Debug.Log("CRASHLAB::json_parse_error::START");
            // Simulate common data parsing failure
            var dt = DateTime.ParseExact("not-a-date", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            Debug.Log(dt.ToString());
        }

        public static void UseAfterDispose()
        {
            Debug.Log("CRASHLAB::use_after_dispose::START");
            var ms = new System.IO.MemoryStream(new byte[] { 1, 2, 3 });
            ms.Dispose();
            var b = ms.ReadByte(); // ObjectDisposedException
            Debug.Log(b);
        }

        public static void ScheduleStartupCrash(string key = "crashlab_startup_action")
        {
            // Use a native abort on next launch for deterministic fatal behavior
            PlayerPrefs.SetString(key, "native_abort");
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
            else if (action == "native_abort") NativeAbort();
        }
    }
}
