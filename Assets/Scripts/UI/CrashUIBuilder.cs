using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrashLab.UI
{
    public class CrashUIBuilder : MonoBehaviour
    {
        [Serializable]
        public enum ActionType
        {
            ManagedNullRef,
            ManagedDivZero,
            ManagedUnhandled,
            ManagedUnobservedTask,
            NativeAccessViolation,
            NativeAbort,
            NativeFatal,
            NativeStackOverflow,
            AndroidAnr10,
            DesktopHang10,
            OomHeap,
            ScheduleStartupCrash
        }

        [Serializable]
        public struct ButtonEntry
        {
            public string label;
            public ActionType action;
        }

        [Header("Layout & Prefab")]
        [SerializeField] private RectTransform _content;
        [SerializeField] private CrashUIButton _buttonPrefab;

        [Header("Buttons")] 
        [SerializeField] private List<ButtonEntry> _buttons = new List<ButtonEntry>();

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

            // Clear old children (designer controls layout container; we only populate)
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                var child = _content.GetChild(i);
                if (Application.isEditor)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
            }

            foreach (var entry in _buttons)
            {
                var instance = Instantiate(_buttonPrefab, _content);
                instance.Setup(entry.label, Resolve(entry.action));
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
                case ActionType.NativeAccessViolation: return CrashActions.NativeAccessViolation;
                case ActionType.NativeAbort: return CrashActions.NativeAbort;
                case ActionType.NativeFatal: return CrashActions.NativeFatal;
                case ActionType.NativeStackOverflow: return CrashActions.NativeStackOverflow;
                case ActionType.AndroidAnr10: return () => CrashActions.AndroidAnr(10);
                case ActionType.DesktopHang10: return () => CrashActions.DesktopHang(10);
                case ActionType.OomHeap: return CrashActions.OomHeap;
                case ActionType.ScheduleStartupCrash: return () => CrashActions.ScheduleStartupCrash();
                default: return null;
            }
        }

        // Optionally seed a useful default list when first added
        private void Reset()
        {
            if (_buttons == null || _buttons.Count > 0) return;
            _buttons = new List<ButtonEntry>
            {
                new ButtonEntry { label = "Managed: NullRef", action = ActionType.ManagedNullRef },
                new ButtonEntry { label = "Managed: DivZero", action = ActionType.ManagedDivZero },
                new ButtonEntry { label = "Managed: Unhandled", action = ActionType.ManagedUnhandled },
                new ButtonEntry { label = "Managed: Unobserved Task", action = ActionType.ManagedUnobservedTask },
                new ButtonEntry { label = "Native: AccessViolation", action = ActionType.NativeAccessViolation },
                new ButtonEntry { label = "Native: Abort", action = ActionType.NativeAbort },
                new ButtonEntry { label = "Native: FatalError", action = ActionType.NativeFatal },
                new ButtonEntry { label = "Native: StackOverflow", action = ActionType.NativeStackOverflow },
                new ButtonEntry { label = "Hang: Android ANR (10s)", action = ActionType.AndroidAnr10 },
                new ButtonEntry { label = "Hang: Desktop (10s)", action = ActionType.DesktopHang10 },
                new ButtonEntry { label = "OOM: Heap", action = ActionType.OomHeap },
                new ButtonEntry { label = "Schedule: Startup crash", action = ActionType.ScheduleStartupCrash },
            };
        }
    }
}

