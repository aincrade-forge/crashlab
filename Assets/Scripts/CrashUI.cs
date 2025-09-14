using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrashLab
{
    public class CrashUI : MonoBehaviour
    {
        private readonly List<(string, System.Action)> _actions = new List<(string, System.Action)>
        {
            ("Managed: NullRef", CrashActions.ManagedNullRef),
            ("Managed: DivZero", CrashActions.ManagedDivZero),
            ("Managed: Unhandled", CrashActions.ManagedUnhandled),
            ("Managed: Unobserved Task", CrashActions.ManagedUnobservedTask),
            ("Native: AccessViolation", CrashActions.NativeAccessViolation),
            ("Native: Abort", CrashActions.NativeAbort),
            ("Native: FatalError", CrashActions.NativeFatal),
            ("Native: StackOverflow", CrashActions.NativeStackOverflow),
            ("Hang: Android ANR (10s)", () => CrashActions.AndroidAnr(10)),
            ("Hang: Desktop (10s)", () => CrashActions.DesktopHang(10)),
            ("OOM: Heap", CrashActions.OomHeap),
            ("Schedule: Startup crash", CrashActions.ScheduleStartupCrash),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            CrashActions.CheckAndRunStartupCrash();

            var go = new GameObject("CrashLabUI");
            DontDestroyOnLoad(go);
            go.AddComponent<CrashUI>();
        }

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

            foreach (var (label, action) in _actions)
            {
                var btn = CreateButton(panel.transform, label);
                btn.onClick.AddListener(() => action());
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

