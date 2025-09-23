using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrashLab.UI
{
    public class CrashUIBuilder : MonoBehaviour
    {
        [Serializable]
        public enum Group
        {
            Crashes,
            Errors
        }

        [Serializable]
        public enum ActionType
        {
            ManagedNullRef,
            ManagedDivZero,
            ManagedUnhandled,
            ManagedUnobservedTask,
            ManagedIndexOutOfRange,
            ManagedKeyNotFound,
            ManagedInvalidOperation_ModifiedDuringEnumeration,
            ManagedAggregate,
            NativeAccessViolation,
            NativeAbort,
            NativeFatal,
            NativeStackOverflow,
            AndroidAnr10,
            DesktopHang10,
            SyncWaitHang10,
            OomHeap,
            FileWriteDenied,
            JsonParseError,
            UseAfterDispose,
            BackgroundThreadUnhandled,
            ThreadPoolUnhandled,
            UnityApiFromWorker,
#if DIAG_SENTRY
            SentrySelfTest,
#endif
            ScheduleStartupCrash
        }

        [Serializable]
        public struct ButtonEntry
        {
            public string label;
            public ActionType action;
        }

        [Header("Layout & Prefab")]
        [SerializeField] private RectTransform _content; // optional fallback
        [SerializeField] private CrashUIButton _buttonPrefab;

        [Header("Targets (assign both)")]
        [SerializeField] private RectTransform _crashesContent;
        [SerializeField] private RectTransform _errorsContent;

        // Buttons are generated at runtime on each Build() call; no cached serialized list

        private void Awake()
        {
            Build();
        }

        public void Build()
        {
            if (_content == null || _buttonPrefab == null)
            {
                Debug.LogWarning("CrashUIBuilder: Missing content container or button prefab.");
                return;
            }

            // Validate
            if (_buttonPrefab == null)
            {
                Debug.LogWarning("CrashUIBuilder: Missing button prefab.");
                return;
            }

            // Clear old children for both targets if assigned
            ClearChildren(_crashesContent);
            ClearChildren(_errorsContent);
            if (_crashesContent == null || _errorsContent == null)
            {
                Debug.Log("CrashUIBuilder: Assign both Crashes Content and Errors Content. Using fallback content if provided.");
                ClearChildren(_content);
            }

            foreach (var entry in BuildButtonList())
            {
                var group = MapGroup(entry.action);
                RectTransform parent = null;
                if (group == Group.Crashes) parent = _crashesContent;
                else parent = _errorsContent;

                // Fallback
                if (parent == null) parent = _content;
                if (parent == null)
                {
                    Debug.LogWarning($"CrashUIBuilder: No parent assigned for {group} → Skipping '{entry.label}'");
                    continue;
                }

                var instance = Instantiate(_buttonPrefab, parent);
                instance.Setup(entry.label, Resolve(entry.action));
            }
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (Application.isEditor)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                else
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static Action Resolve(ActionType type)
        {
            switch (type)
            {
                case ActionType.ManagedNullRef: return CrashActions.ManagedNullRef;
                case ActionType.ManagedDivZero: return CrashActions.ManagedDivZero;
                case ActionType.ManagedUnhandled: return CrashActions.ManagedUnhandled;
                case ActionType.ManagedUnobservedTask: return CrashActions.ManagedUnobservedTask;
                case ActionType.ManagedIndexOutOfRange: return CrashActions.ManagedIndexOutOfRange;
                case ActionType.ManagedKeyNotFound: return CrashActions.ManagedKeyNotFound;
                case ActionType.ManagedInvalidOperation_ModifiedDuringEnumeration: return CrashActions.ManagedInvalidOperation_ModifiedDuringEnumeration;
                case ActionType.ManagedAggregate: return CrashActions.ManagedAggregate;
                case ActionType.NativeAccessViolation: return CrashActions.NativeAccessViolation;
                case ActionType.NativeAbort: return CrashActions.NativeAbort;
                case ActionType.NativeFatal: return CrashActions.NativeFatal;
                case ActionType.NativeStackOverflow: return CrashActions.NativeStackOverflow;
                case ActionType.AndroidAnr10: return () => CrashActions.AndroidAnr(10);
                case ActionType.DesktopHang10: return () => CrashActions.DesktopHang(10);
                case ActionType.SyncWaitHang10: return () => CrashActions.SyncWaitHang(10);
                case ActionType.OomHeap: return CrashActions.OomHeap;
                case ActionType.FileWriteDenied: return CrashActions.FileWriteDenied;
                case ActionType.JsonParseError: return CrashActions.JsonParseError;
                case ActionType.UseAfterDispose: return CrashActions.UseAfterDispose;
                case ActionType.BackgroundThreadUnhandled: return CrashActions.BackgroundThreadUnhandled;
                case ActionType.ThreadPoolUnhandled: return CrashActions.ThreadPoolUnhandled;
                case ActionType.UnityApiFromWorker: return CrashActions.UnityApiFromWorker;
#if DIAG_SENTRY
                case ActionType.SentrySelfTest: return CrashActions.SentrySelfTest;
#endif
                case ActionType.ScheduleStartupCrash: return () => CrashActions.ScheduleStartupCrash();
                default: return null;
            }
        }

        private static Group MapGroup(ActionType type)
        {
            switch (type)
            {
                // Crashes: highly likely to terminate the app immediately
                case ActionType.ManagedNullRef:
                case ActionType.ManagedDivZero:
                case ActionType.ManagedUnhandled:
                case ActionType.ManagedAggregate:
                case ActionType.NativeAccessViolation:
                case ActionType.NativeAbort:
                case ActionType.NativeFatal:
                case ActionType.NativeStackOverflow:
                    return Group.Crashes;

                // Everything else: Errors (hanging, threading issues, OOM, IO/Data, diagnostics, scheduling)
                default:
                    return Group.Errors;
            }
        }

        // No headers/labels; buttons go directly into the assigned containers

        private IEnumerable<ButtonEntry> BuildButtonList()
        {
            yield return new ButtonEntry { label = "Managed: NullRef", action = ActionType.ManagedNullRef };
            yield return new ButtonEntry { label = "Managed: DivZero", action = ActionType.ManagedDivZero };
            yield return new ButtonEntry { label = "Managed: Unhandled", action = ActionType.ManagedUnhandled };
            yield return new ButtonEntry { label = "Managed: Unobserved Task", action = ActionType.ManagedUnobservedTask };
            yield return new ButtonEntry { label = "Managed: IndexOutOfRange", action = ActionType.ManagedIndexOutOfRange };
            yield return new ButtonEntry { label = "Managed: KeyNotFound", action = ActionType.ManagedKeyNotFound };
            yield return new ButtonEntry { label = "Managed: InvalidOperation (Modify During Enum)", action = ActionType.ManagedInvalidOperation_ModifiedDuringEnumeration };
            yield return new ButtonEntry { label = "Managed: AggregateException", action = ActionType.ManagedAggregate };
            yield return new ButtonEntry { label = "Native: AccessViolation", action = ActionType.NativeAccessViolation };
            yield return new ButtonEntry { label = "Native: Abort", action = ActionType.NativeAbort };
            yield return new ButtonEntry { label = "Native: FatalError", action = ActionType.NativeFatal };
            yield return new ButtonEntry { label = "Native: StackOverflow", action = ActionType.NativeStackOverflow };
            yield return new ButtonEntry { label = "Hang: Android ANR (10s)", action = ActionType.AndroidAnr10 };
            yield return new ButtonEntry { label = "Hang: Desktop (10s)", action = ActionType.DesktopHang10 };
            yield return new ButtonEntry { label = "Hang: Sync Wait (10s)", action = ActionType.SyncWaitHang10 };
            yield return new ButtonEntry { label = "OOM: Heap", action = ActionType.OomHeap };
            yield return new ButtonEntry { label = "IO: File Write Denied", action = ActionType.FileWriteDenied };
            yield return new ButtonEntry { label = "Data: JSON Parse Error", action = ActionType.JsonParseError };
            yield return new ButtonEntry { label = "Lifecycle: Use After Dispose", action = ActionType.UseAfterDispose };
            yield return new ButtonEntry { label = "Thread: Background Unhandled", action = ActionType.BackgroundThreadUnhandled };
            yield return new ButtonEntry { label = "Thread: ThreadPool Unhandled", action = ActionType.ThreadPoolUnhandled };
            yield return new ButtonEntry { label = "Thread: Unity API From Worker", action = ActionType.UnityApiFromWorker };
#if DIAG_SENTRY
            yield return new ButtonEntry { label = "Sentry: Self-test event", action = ActionType.SentrySelfTest };
#endif
            yield return new ButtonEntry { label = "Schedule: Startup crash", action = ActionType.ScheduleStartupCrash };
        }
    }
}
