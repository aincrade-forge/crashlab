using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if UNITY_IOS || UNITY_TVOS || UNITY_ANDROID
using AOT;
#endif
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Diagnostics;
using CrashLab.Actions;

namespace CrashLab
{
    public static class CrashActions
    {
        private static readonly List<byte[]> _oom = new List<byte[]>();

#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX || UNITY_ANDROID) && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void throw_cpp();

        [DllImport("__Internal")]
        private static extern void crash_in_cpp();

        [DllImport("__Internal")]
        private static extern void crash_in_c();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NativeCallback(int code);

        [DllImport("__Internal")]
        private static extern void call_into_csharp(NativeCallback callback);

#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void throwObjectiveC();
#endif

#if (UNITY_IOS || UNITY_TVOS || UNITY_ANDROID)
        [AOT.MonoPInvokeCallback(typeof(NativeCallback))]
#endif
        private static void NativeCallbackHandler(int code)
        {
            throw new Exception($"CrashLab native callback exception (code={code})");
        }
#endif

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

        public static void NativeForceCrash()
        {
            Debug.Log("CRASHLAB::native_force_crash::START");
#if UNITY_EDITOR
            Debug.LogWarning("Unity Diagnostics ForceCrash is disabled in the Editor to avoid shutting down Unity.");
#else
            Utils.ForceCrash(ForcedCrashCategory.AccessViolation);
#endif
        }

        public static void NativeThrowCpp()
        {
            Debug.Log("CRASHLAB::native_throw_cpp::START");
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX || UNITY_ANDROID) && !UNITY_EDITOR
            throw_cpp();
#else
            Debug.LogWarning("Native C++ throw requires an IL2CPP player build.");
#endif
        }

        public static void NativeCrashInCpp()
        {
            Debug.Log("CRASHLAB::native_crash_in_cpp::START");
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX || UNITY_ANDROID) && !UNITY_EDITOR
            crash_in_cpp();
#else
            Debug.LogWarning("Native crash_in_cpp requires an IL2CPP player build.");
#endif
        }

        public static void NativeCrashInC()
        {
            Debug.Log("CRASHLAB::native_crash_in_c::START");
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX || UNITY_ANDROID) && !UNITY_EDITOR
            crash_in_c();
#else
            Debug.LogWarning("Native crash_in_c requires an IL2CPP player build.");
#endif
        }

        public static void NativeCallbackException()
        {
            Debug.Log("CRASHLAB::native_callback_exception::START");
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX || UNITY_ANDROID) && !UNITY_EDITOR
            try
            {
                call_into_csharp(NativeCallbackHandler);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
#else
            Debug.LogWarning("Native callback exception requires an IL2CPP player build.");
#endif
        }

        public static void AndroidThrowKotlin()
        {
            Debug.Log("CRASHLAB::android_throw_kotlin::START");
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var plugin = new AndroidJavaObject("unity.of.bugs.KotlinPlugin"))
            {
                plugin.CallStatic("throw");
            }
#else
            Debug.LogWarning("Kotlin throw is only available on Android devices.");
#endif
        }

        public static void AndroidThrowKotlinBackground()
        {
            Debug.Log("CRASHLAB::android_throw_kotlin_bg::START");
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var plugin = new AndroidJavaObject("unity.of.bugs.KotlinPlugin"))
            {
                plugin.CallStatic("throwOnBackgroundThread");
            }
#else
            Debug.LogWarning("Kotlin background throw is only available on Android devices.");
#endif
        }

        public static void AndroidOomKotlin()
        {
            Debug.Log("CRASHLAB::android_oom_kotlin::START");
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var plugin = new AndroidJavaObject("unity.of.bugs.KotlinPlugin"))
            {
                plugin.CallStatic("oom");
            }
#else
            Debug.LogWarning("Kotlin OOM is only available on Android devices.");
#endif
        }

        public static void AndroidOomKotlinBackground()
        {
            Debug.Log("CRASHLAB::android_oom_kotlin_bg::START");
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var plugin = new AndroidJavaObject("unity.of.bugs.KotlinPlugin"))
            {
                plugin.CallStatic("oomOnBackgroundThread");
            }
#else
            Debug.LogWarning("Kotlin background OOM is only available on Android devices.");
#endif
        }

        public static void IosThrowObjectiveC()
        {
            Debug.Log("CRASHLAB::ios_throw_objc::START");
#if UNITY_IOS && !UNITY_EDITOR
            throwObjectiveC();
#else
            Debug.LogWarning("Objective-C throw is only available on iOS builds.");
#endif
        }

        public static void IosRunOutOfMemory()
        {
            Debug.Log("CRASHLAB::ios_oom_native::START");
#if UNITY_IOS && !UNITY_EDITOR
            const int blockSize = 32 * 1024 * 1024;
            try
            {
                while (true)
                {
                    var block = new byte[blockSize];
                    _oom.Add(block);
                }
            }
            catch (OutOfMemoryException)
            {
                Environment.FailFast("CrashLab: native-inspired OOM");
            }
#else
            Debug.LogWarning("iOS OOM native scenario is only meaningful on iOS builds.");
#endif
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
                // Accessing Unity APIs off main thread is unsafe; expect errors/logs, not guaranteed crash
                var cam = Camera.main; // may be null or trigger warning
                Debug.Log($"Worker accessed Camera.main: {(cam ? cam.name : "<null>")}");
                Resources.Load("NonExistent");
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
            CrashNative.IllegalInstruction();
        }

        public static void NativeStackOverflow()
        {
            Debug.Log("CRASHLAB::native_stack_overflow::START");
            var thread = new Thread(StackOverflowThread, 64 * 1024)
            {
                IsBackground = true,
                Name = "CrashLabStackOverflow"
            };
            thread.Start();
            thread.Join();
        }

        private static void StackOverflowThread()
        {
            StackOverflowRecursive(0);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void StackOverflowRecursive(int depth)
        {
            StackOverflowRecursive(depth + 1);
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
            const int initialSize = 64 * 1024 * 1024;
            const int maxSize = 512 * 1024 * 1024;
            var size = initialSize;
            long total = 0;
            var allocations = 0;
            while (true)
            {
                var block = new byte[size];
                _oom.Add(block);
                allocations++;
                total += block.LongLength;

                if (allocations <= 4 || allocations % 4 == 0)
                {
                    Debug.Log($"CRASHLAB::oom_heap::ALLOC::{size / (1024 * 1024)}MB::TOTAL::{total / (1024 * 1024)}MB");
                }

                if (size < maxSize)
                {
                    size = Math.Min(size * 2, maxSize);
                }
            }
        }

        public static void AssetBundleFlood()
        {
            Debug.Log("CRASHLAB::asset_bundle_flood::START");
            AssetBundleFloodRunner.Run();
        }

        public static void AssetBundleFloodEditor()
        {
#if UNITY_EDITOR
            Debug.Log("CRASHLAB::asset_bundle_flood_editor::START");
            AssetBundleFloodRunner.Run(false);
#else
            Debug.LogWarning("CRASHLAB::asset_bundle_flood_editor requested outside editor; skipping");
#endif
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
