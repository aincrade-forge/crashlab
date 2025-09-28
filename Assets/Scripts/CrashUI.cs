using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrashLab
{
    public class CrashUI : MonoBehaviour
    {
        private struct ActionItem
        {
            public string Label;
            public System.Action Handler;
            public ActionItem(string label, System.Action handler)
            {
                Label = label; Handler = handler;
            }
        }

        private readonly List<ActionItem> _actions = new List<ActionItem>
        {
            new ActionItem("Managed: NullRef", CrashActions.ManagedNullRef),
            new ActionItem("Managed: DivZero", CrashActions.ManagedDivZero),
            new ActionItem("Managed: Unhandled", CrashActions.ManagedUnhandled),
            new ActionItem("Managed: Unobserved Task", CrashActions.ManagedUnobservedTask),
            new ActionItem("Native: AccessViolation", CrashActions.NativeAccessViolation),
            new ActionItem("Native: Abort", CrashActions.NativeAbort),
            new ActionItem("Native: FatalError", CrashActions.NativeFatal),
            new ActionItem("Native: StackOverflow", CrashActions.NativeStackOverflow),
            new ActionItem("Hang: Android ANR (10s)", () => CrashActions.AndroidAnr(10)),
            new ActionItem("Hang: Desktop (10s)", () => CrashActions.DesktopHang(10)),
            new ActionItem("OOM: Heap", CrashActions.OomHeap),
            new ActionItem("Memory: Asset bundle flood", CrashActions.AssetBundleFlood),
            new ActionItem("Schedule: Startup crash", () => CrashActions.ScheduleStartupCrash()),
        };

        // Legacy auto-install UI removed. Use CrashLab.UI.CrashUIBuilder in-scene instead.

        private void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = CreateUIObject("Panel", canvasGo);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.35f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0.45f, 1f);
            rect.offsetMin = new Vector2(8, 8);
            rect.offsetMax = new Vector2(-8, -8);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6f;
            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var item in _actions)
            {
                var btn = CreateButton(panel.transform, item.Label);
                var handler = item.Handler; // avoid modified closure
                btn.onClick.AddListener(() => handler());
            }

            var info = CreateText(panel.transform, "Info", 12, TextAnchor.UpperLeft);
            info.text = "CrashLab UI\nUse buttons to trigger actions.\n" +
                        "Actions log to Console with CRASHLAB::<ACTION>::START.";
        }

        private static GameObject CreateUIObject(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }

        private static Button CreateButton(Transform parent, string label)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.9f, 0.85f);
            var btn = go.AddComponent<Button>();

            var text = CreateText(go.transform, label, 14, TextAnchor.MiddleCenter);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 36);
            return btn;
        }

        private static Text CreateText(Transform parent, string content, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.color = Color.white;
            txt.alignment = anchor;
            txt.fontSize = size;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 24);
            return txt;
        }
    }
}
