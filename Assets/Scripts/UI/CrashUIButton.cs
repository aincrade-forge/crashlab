using System;
using UnityEngine;
using UnityEngine.UI;

namespace CrashLab.UI
{
    [RequireComponent(typeof(Button))]
    public class CrashUIButton : MonoBehaviour
    {
        [SerializeField] private Text _label;
        [SerializeField] private Button _button;

        private void Reset()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_label == null)
            {
                _label = GetComponentInChildren<Text>();
            }
        }

        public void Setup(string label, Action onClick)
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_label != null) _label.text = label;
            _button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                _button.onClick.AddListener(() => onClick());
            }
        }
    }
}

