using System;
using UnityEngine;

namespace CrashLab
{
    public class CrashHeadlessTriggers : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            CrashActions.CheckAndRunStartupCrash();
            var go = new GameObject("CrashLabHeadless");
            DontDestroyOnLoad(go);
            go.AddComponent<CrashHeadlessTriggers>();
        }

        private void Awake()
        {
            // Android intent extra: crash_action
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
                {
                    var action = intent.Call<string>("getStringExtra", "crash_action");
                    if (!string.IsNullOrEmpty(action))
                    {
                        Debug.Log($"CRASHLAB::INTENT::crash_action={action}");
                        InvokeAction(action);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CrashHeadlessTriggers Android intent error: {e.Message}");
            }
#endif

            // Deep link (iOS/Android): crashlab://action/<ACTION>
            Application.deepLinkActivated += OnDeepLink;
        }

        private void OnDestroy()
        {
            Application.deepLinkActivated -= OnDeepLink;
        }

        private void OnDeepLink(string url)
        {
            try
            {
                Debug.Log($"CRASHLAB::DEEPLINK::{url}");
                var action = ParseActionFromUrl(url);
                if (!string.IsNullOrEmpty(action))
                {
                    InvokeAction(action);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CrashHeadlessTriggers deep link error: {e.Message}");
            }
        }

        private static string ParseActionFromUrl(string url)
        {
            // Expect formats like: crashlab://action/<ACTION>
            // Simple parse to extract last segment
            if (string.IsNullOrEmpty(url)) return null;
            var idx = url.IndexOf("action/", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var part = url.Substring(idx + "action/".Length);
            // Trim any query or fragment
            var q = part.IndexOfAny(new[] { '?', '#', '&' });
            if (q >= 0) part = part.Substring(0, q);
            return part;
        }

        private static void InvokeAction(string action)
        {
            switch (action)
            {
                case "managed_exception":
                case "managed_unhandled":
                    CrashActions.ManagedUnhandled();
                    break;
                case "managed_null_ref":
                    CrashActions.ManagedNullRef();
                    break;
                case "managed_div_zero":
                    CrashActions.ManagedDivZero();
                    break;
                case "unobserved_task":
                case "managed_unobserved_task":
                    CrashActions.ManagedUnobservedTask();
                    break;
                case "native_av":
                    CrashActions.NativeAccessViolation();
                    break;
                case "native_abort":
                    CrashActions.NativeAbort();
                    break;
                case "native_fatal":
                    CrashActions.NativeFatal();
                    break;
                case "native_stack_overflow":
                    CrashActions.NativeStackOverflow();
                    break;
                case "android_anr":
                    CrashActions.AndroidAnr(10);
                    break;
                case "desktop_hang":
                    CrashActions.DesktopHang(10);
                    break;
                case "oom_heap":
                    CrashActions.OomHeap();
                    break;
                default:
                    Debug.LogWarning($"CRASHLAB::ACTION::UNKNOWN::{action}");
                    break;
            }
        }
    }
}
