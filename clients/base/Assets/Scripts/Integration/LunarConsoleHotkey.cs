using UnityEngine;

namespace CrashLab
{
    public class LunarConsoleHotkey : MonoBehaviour
    {
        [SerializeField] private KeyCode _key = KeyCode.BackQuote; // '~' on most layouts
        [SerializeField] private float _openTimeoutSeconds = 1.0f;
        private bool _opened;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("LunarConsoleHotkey");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<LunarConsoleHotkey>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_key))
            {
                // Only opens the console; use in-app UI to close
                StartCoroutine(OpenAndVerify());
            }
        }

        private System.Collections.IEnumerator OpenAndVerify()
        {
            _opened = false;
            // Subscribe once for this attempt
            System.Action onOpen = () => { _opened = true; };
            LunarConsolePlugin.LunarConsole.onConsoleOpened += onOpen;

            // Call show and wait for the open callback or timeout
            LunarConsolePlugin.LunarConsole.Show();
            float deadline = Time.unscaledTime + _openTimeoutSeconds;
            while (!_opened && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            LunarConsolePlugin.LunarConsole.onConsoleOpened -= onOpen;

            if (!_opened)
            {
                Debug.LogWarning("LunarConsoleHotkey: Console did not open within timeout. Retrying once.");
                LunarConsolePlugin.LunarConsole.Show();
            }
        }
    }
}
