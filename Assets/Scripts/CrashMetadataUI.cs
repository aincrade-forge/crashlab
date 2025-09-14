using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if DIAG_SENTRY
using Sentry;
#endif

#if DIAG_CRASHLYTICS
using Firebase.Crashlytics;
#endif

#if DIAG_UNITY
using Unity.Services.CloudDiagnostics;
#endif

namespace CrashLab
{
    public class CrashMetadataUI : MonoBehaviour
    {
        private InputField _userId;
        private InputField _runId;
        private InputField _environment;
        private InputField _commitSha;
        private InputField _buildNumber;
        private InputField _devMode;
        private InputField _serverName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("CrashLabMetadataUI");
            DontDestroyOnLoad(go);
            go.AddComponent<CrashMetadataUI>();
        }

        private void Awake()
        {
            BuildUI();
            LoadFromPrefs();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = CreateUIObject("MetadataPanel", canvasGo);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.35f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.55f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(8, 8);
            rect.offsetMax = new Vector2(-8, -8);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6f;
            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateText(panel.transform, "CrashLab Metadata", 16, TextAnchor.MiddleLeft);

            _userId = LabeledInput(panel.transform, "user.id");
            _runId = LabeledInput(panel.transform, "run_id");
            _environment = LabeledInput(panel.transform, "environment");
            _commitSha = LabeledInput(panel.transform, "commit_sha");
            _buildNumber = LabeledInput(panel.transform, "build_number");
            _devMode = LabeledInput(panel.transform, "dev_mode (true/false)");
            _serverName = LabeledInput(panel.transform, "server_name");

            var btnRow = CreateUIObject("Buttons", panel);
            var hLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 6f;
            hLayout.childForceExpandHeight = false;
            hLayout.childForceExpandWidth = false;

            var apply = CreateButton(btnRow.transform, "Apply", OnApply);
            var load = CreateButton(btnRow.transform, "Load", _ => LoadFromPrefs());
            var reset = CreateButton(btnRow.transform, "Reset", _ => ResetDefaults());
        }

        private void OnApply()
        {
            var userId = _userId.text;
            var runId = _runId.text;
            var env = _environment.text;
            var sha = _commitSha.text;
            var build = _buildNumber.text;
            var dev = _devMode.text;
            var server = _serverName.text;

            // Persist
            PlayerPrefs.SetString("crashlab_user_id", userId);
            PlayerPrefs.SetString("crashlab_run_id", runId);
            PlayerPrefs.SetString("crashlab_environment", env);
            PlayerPrefs.SetString("crashlab_commit_sha", sha);
            PlayerPrefs.SetString("crashlab_build_number", build);
            PlayerPrefs.SetString("crashlab_dev_mode", dev);
            PlayerPrefs.SetString("crashlab_server_name", server);
            PlayerPrefs.Save();

            // Backend updates
#if DIAG_SENTRY
            try
            {
                SentrySdk.ConfigureScope(s =>
                {
                    s.User = new User { Id = userId };
                    s.Environment = env;
                    s.SetTag("run_id", runId);
                    s.SetTag("commit_sha", sha);
                    s.SetTag("build_number", build);
                    s.SetTag("dev_mode", dev);
                    s.SetTag("server_name", server);
                    s.SetTag("backend", "sentry");
                    s.SetTag("platform", Application.platform.ToString().ToLowerInvariant());
                });
                Debug.Log("CRASHLAB::META::APPLIED::sentry");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Sentry metadata apply failed: {e.Message}");
            }
#endif

#if DIAG_CRASHLYTICS
            try
            {
                Crashlytics.SetUserId(userId);
                Crashlytics.SetCustomKey("run_id", runId);
                Crashlytics.SetCustomKey("commit_sha", sha);
                Crashlytics.SetCustomKey("build_number", build);
                Crashlytics.SetCustomKey("dev_mode", dev);
                Crashlytics.SetCustomKey("server_name", server);
                Crashlytics.SetCustomKey("backend", "crashlytics");
                Crashlytics.SetCustomKey("platform", Application.platform.ToString().ToLowerInvariant());
                Crashlytics.Log("CrashLab metadata applied");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Crashlytics metadata apply failed: {e.Message}");
            }
#endif

#if DIAG_UNITY
            try
            {
                var md = new Dictionary<string, object>
                {
                    ["run_id"] = runId,
                    ["commit_sha"] = sha,
                    ["build_number"] = build,
                    ["dev_mode"] = dev,
                    ["server_name"] = server,
                    ["backend"] = "unity",
                    ["platform"] = Application.platform.ToString().ToLowerInvariant(),
                    ["environment"] = env,
                };
                CloudDiagnostics.CrashReporting.SetUserId(userId);
                CloudDiagnostics.CrashReporting.SetCustomMetadata(md);
                Debug.Log("CRASHLAB::META::APPLIED::unity");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Unity Diagnostics metadata apply failed: {e.Message}");
            }
#endif
        }

        private void LoadFromPrefs()
        {
            _userId.text = PlayerPrefs.GetString("crashlab_user_id", Guid.NewGuid().ToString("N"));
            _runId.text = PlayerPrefs.GetString("crashlab_run_id", DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
            _environment.text = PlayerPrefs.GetString("crashlab_environment", "dev");
            _commitSha.text = PlayerPrefs.GetString("crashlab_commit_sha", "local");
            _buildNumber.text = PlayerPrefs.GetString("crashlab_build_number", "1");
            _devMode.text = PlayerPrefs.GetString("crashlab_dev_mode", Debug.isDebugBuild ? "true" : "false");
            _serverName.text = PlayerPrefs.GetString("crashlab_server_name", SystemInfo.deviceName);
        }

        private void ResetDefaults()
        {
            _runId.text = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            _devMode.text = Debug.isDebugBuild ? "true" : "false";
            _environment.text = "dev";
            _commitSha.text = "local";
            _buildNumber.text = "1";
            _serverName.text = SystemInfo.deviceName;
        }

        private static GameObject CreateUIObject(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            return go;
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

        private static InputField LabeledInput(Transform parent, string label)
        {
            CreateText(parent, label, 12, TextAnchor.LowerLeft);
            var go = new GameObject("Input");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var input = go.AddComponent<InputField>();

            var ph = new GameObject("Placeholder");
            ph.transform.SetParent(go.transform, false);
            var phText = ph.AddComponent<Text>();
            phText.text = "";
            phText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            phText.fontStyle = FontStyle.Italic;
            phText.color = new Color(1, 1, 1, 0.5f);

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.color = Color.white;

            input.textComponent = txt;
            input.placeholder = phText;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 28);
            return input;
        }

        private static Button CreateButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 0.3f, 0.85f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());

            var text = new GameObject("Text");
            text.transform.SetParent(go.transform, false);
            var txt = text.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 32);
            return btn;
        }
    }
}

