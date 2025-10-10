using System;
using System.Collections;
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
            AssetBundleFlood,
            ScheduleStartupCrash,
            NonFatalErrorChain
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

        [Header("Error Chain")]
        [SerializeField, Range(0.5f, 10f)] private float _errorChainDelaySeconds = 2.5f;
        [SerializeField] private CrashUIButton _errorChainButton;

        private Coroutine _errorChainRoutine;

        private const string ErrorChainCategory = "crashlab.error_chain";

        private static readonly ButtonEntry[] ButtonDefinitions =
        {
            new ButtonEntry { label = "Managed: NullRef", action = ActionType.ManagedNullRef },
            new ButtonEntry { label = "Managed: DivZero", action = ActionType.ManagedDivZero },
            new ButtonEntry { label = "Managed: Unhandled", action = ActionType.ManagedUnhandled },
            new ButtonEntry { label = "Managed: Unobserved Task", action = ActionType.ManagedUnobservedTask },
            new ButtonEntry { label = "Managed: IndexOutOfRange", action = ActionType.ManagedIndexOutOfRange },
            new ButtonEntry { label = "Managed: KeyNotFound", action = ActionType.ManagedKeyNotFound },
            new ButtonEntry { label = "Managed: InvalidOperation (Modify During Enum)", action = ActionType.ManagedInvalidOperation_ModifiedDuringEnumeration },
            new ButtonEntry { label = "Managed: AggregateException", action = ActionType.ManagedAggregate },
            new ButtonEntry { label = "Native: AccessViolation", action = ActionType.NativeAccessViolation },
            new ButtonEntry { label = "Native: Abort", action = ActionType.NativeAbort },
            new ButtonEntry { label = "Native: FatalError", action = ActionType.NativeFatal },
            new ButtonEntry { label = "Native: StackOverflow", action = ActionType.NativeStackOverflow },
            new ButtonEntry { label = "Schedule: Startup crash", action = ActionType.ScheduleStartupCrash },
            new ButtonEntry { label = "Hang: Android ANR (10s)", action = ActionType.AndroidAnr10 },
            new ButtonEntry { label = "Hang: Desktop (10s)", action = ActionType.DesktopHang10 },
            new ButtonEntry { label = "Hang: Sync Wait (10s)", action = ActionType.SyncWaitHang10 },
            new ButtonEntry { label = "OOM: Heap", action = ActionType.OomHeap },
            new ButtonEntry { label = "Memory: Asset bundle flood", action = ActionType.AssetBundleFlood },
            new ButtonEntry { label = "IO: File Write Denied", action = ActionType.FileWriteDenied },
            new ButtonEntry { label = "Data: JSON Parse Error", action = ActionType.JsonParseError },
            new ButtonEntry { label = "Lifecycle: Use After Dispose", action = ActionType.UseAfterDispose },
            new ButtonEntry { label = "Thread: Background Unhandled", action = ActionType.BackgroundThreadUnhandled },
            new ButtonEntry { label = "Thread: ThreadPool Unhandled", action = ActionType.ThreadPoolUnhandled },
            new ButtonEntry { label = "Thread: Unity API From Worker", action = ActionType.UnityApiFromWorker }
        };

        private static readonly ButtonEntry[] ErrorChainEntries = BuildErrorChainEntries();

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
            var chainTransform = _errorChainButton != null ? _errorChainButton.transform : null;

            ClearChildren(_crashesContent);
            ClearChildren(_errorsContent, chainTransform);
            if (_crashesContent == null || _errorsContent == null)
            {
                Debug.Log("CrashUIBuilder: Assign both Crashes Content and Errors Content. Using fallback content if provided.");
                ClearChildren(_content, chainTransform);
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

            ConfigureErrorChainButton();
        }

        private void ConfigureErrorChainButton()
        {
            if (_errorChainButton == null)
            {
                return;
            }

            _errorChainButton.Setup("Raise Errors Chain", TriggerErrorChain);
        }

        private void ClearChildren(RectTransform parent, Transform exclude = null)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (exclude != null && (child == exclude || exclude.IsChildOf(child))) continue;
                if (Application.isEditor)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                else
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private Action Resolve(ActionType type)
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
                case ActionType.AssetBundleFlood: return CrashActions.AssetBundleFlood;
                case ActionType.FileWriteDenied: return CrashActions.FileWriteDenied;
                case ActionType.JsonParseError: return CrashActions.JsonParseError;
                case ActionType.UseAfterDispose: return CrashActions.UseAfterDispose;
                case ActionType.BackgroundThreadUnhandled: return CrashActions.BackgroundThreadUnhandled;
                case ActionType.ThreadPoolUnhandled: return CrashActions.ThreadPoolUnhandled;
                case ActionType.UnityApiFromWorker: return CrashActions.UnityApiFromWorker;
                case ActionType.ScheduleStartupCrash: return () => CrashActions.ScheduleStartupCrash();
                case ActionType.NonFatalErrorChain: return TriggerErrorChain;
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
                case ActionType.ScheduleStartupCrash:
                case ActionType.OomHeap:
                    return Group.Crashes;

                // Everything else: Errors (hanging, threading issues, OOM, IO/Data, diagnostics, scheduling)
                default:
                    return Group.Errors;
            }
        }

        // No headers/labels; buttons go directly into the assigned containers

        private void TriggerErrorChain()
        {
            if (!isActiveAndEnabled)
            {
                CrashLabBreadcrumbs.Warning("Error chain requested while builder disabled", ErrorChainCategory);
                return;
            }

            if (_errorChainRoutine != null)
            {
                StopCoroutine(_errorChainRoutine);
                CrashLabBreadcrumbs.Warning("Restarting non-fatal error chain", ErrorChainCategory);
                _errorChainRoutine = null;
            }

            CrashLabTelemetry.EnsureSession("error_chain");
            CrashLabBreadcrumbs.Info("Non-fatal error chain requested", ErrorChainCategory);
            _errorChainRoutine = StartCoroutine(RunErrorChain(ErrorChainCategory));
        }

        private IEnumerator RunErrorChain(string category)
        {
            var steps = ErrorChainEntries;
            var total = steps.Length;

            if (total == 0)
            {
                CrashLabBreadcrumbs.Warning("Error chain requested but no error steps are configured", category);
                Debug.LogWarning("CRASHLAB::ERROR_CHAIN::NO_STEPS");
                _errorChainRoutine = null;
                yield break;
            }

            CrashLabBreadcrumbs.Info($"Non-fatal error chain starting ({total} steps)", category,
                new Dictionary<string, string> { { "steps", total.ToString() } });

            var successLabels = new List<string>(total);
            var failureLabels = new List<string>();

            for (int i = 0; i < total; i++)
            {
                var step = steps[i];
                var index = (i + 1).ToString();
                var stepData = new Dictionary<string, string>
                {
                    { "step_index", index },
                    { "step_label", step.label }
                };

                CrashLabBreadcrumbs.Info($"Step {index}/{total} starting: {step.label}", category, stepData);
                Debug.Log($"CRASHLAB::ERROR_CHAIN::START::{step.label}");

                Exception failure = null;
                try
                {
                    var action = Resolve(step.action);
                    if (action == null)
                    {
                        throw new InvalidOperationException($"No action resolver for {step.action}");
                    }

                    action.Invoke();
                }
                catch (Exception ex)
                {
                    failure = ex;
                    var errorData = new Dictionary<string, string>(stepData)
                    {
                        { "exception_type", ex.GetType().Name },
                        { "exception_message", ex.Message ?? string.Empty }
                    };
                    CrashLabBreadcrumbs.Error($"Step {index}/{total} threw {ex.GetType().Name}", category, errorData);
                    Debug.LogError($"CRASHLAB::ERROR_CHAIN::EXCEPTION::{step.label}::{ex.GetType().Name}:{ex.Message}");
                    Debug.LogException(ex);
                }

                var resultData = new Dictionary<string, string>(stepData)
                {
                    { "status", failure == null ? "ok" : "exception" }
                };
                CrashLabBreadcrumbs.Info($"Step {index}/{total} completed", category, resultData);
                Debug.Log($"CRASHLAB::ERROR_CHAIN::END::{step.label}::{resultData["status"]}");

                if (failure == null)
                {
                    successLabels.Add(step.label);
                }
                else
                {
                    failureLabels.Add(step.label);
                }

                if (i < total - 1 && _errorChainDelaySeconds > 0f)
                {
                    yield return new WaitForSeconds(_errorChainDelaySeconds);
                }
            }

            var summaryData = new Dictionary<string, string>
            {
                { "steps", total.ToString() },
                { "success_count", successLabels.Count.ToString() },
                { "failure_count", failureLabels.Count.ToString() }
            };

            if (successLabels.Count > 0)
            {
                summaryData["success_steps"] = string.Join(",", successLabels);
            }

            if (failureLabels.Count > 0)
            {
                summaryData["failure_steps"] = string.Join(",", failureLabels);
            }

            CrashLabBreadcrumbs.Info("Non-fatal error chain finished", category, summaryData);
            Debug.Log($"CRASHLAB::ERROR_CHAIN::SUMMARY::success={successLabels.Count}:{string.Join("|", successLabels)}::failure={failureLabels.Count}:{string.Join("|", failureLabels)}");
            _errorChainRoutine = null;
        }

        private void OnDisable()
        {
            if (_errorChainRoutine != null)
            {
                StopCoroutine(_errorChainRoutine);
                _errorChainRoutine = null;
                CrashLabBreadcrumbs.Info("Non-fatal error chain cancelled (builder disabled)", ErrorChainCategory);
            }
        }

        private static ButtonEntry[] BuildErrorChainEntries()
        {
            var buffer = new List<ButtonEntry>(ButtonDefinitions.Length);
            for (int i = 0; i < ButtonDefinitions.Length; i++)
            {
                var entry = ButtonDefinitions[i];
                if (MapGroup(entry.action) != Group.Errors)
                {
                    continue;
                }

                buffer.Add(entry);
            }

            return buffer.ToArray();
        }

        private IEnumerable<ButtonEntry> BuildButtonList()
        {
            for (int i = 0; i < ButtonDefinitions.Length; i++)
            {
                yield return ButtonDefinitions[i];
            }
        }
    }
}
