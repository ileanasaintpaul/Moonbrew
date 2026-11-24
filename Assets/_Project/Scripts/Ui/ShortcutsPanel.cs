using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // A reminder of the debug shortcuts, shown and hidden with its key (P). The key of each line is read
    // from its input action, so a rebinding shows up here; a key with no action reference gives its own text.
    [RequireComponent(typeof(UIDocument))]
    public class ShortcutsPanel : MonoBehaviour
    {
        private const string RootName = "shortcuts";

        [SerializeField] private VisualTreeAsset panelTemplate;
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private InputActionReference toggleAction;
        [SerializeField] private Shortcut[] shortcuts = new Shortcut[0];

        private readonly List<Label> _keys = new List<Label>();
        private UIDocument _document;
        private VisualElement _panel;

        public bool IsOpen => _panel != null;

        public int RowCount => _keys.Count;

        public string KeyText(int index) => _keys[index].text;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(VisualTreeAsset template, Shortcut[] lines)
        {
            panelTemplate = template;
            shortcuts = lines ?? new Shortcut[0];
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Close()
        {
            if (_panel == null)
            {
                return;
            }

            _panel.RemoveFromHierarchy();
            _panel = null;
            _keys.Clear();
        }

        // Like the shortcuts it lists, only in the editor and development builds.
        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (!Debug.isDebugBuild)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (toggleAction != null)
            {
                toggleAction.action.performed += OnToggle;
                toggleAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (toggleAction != null)
            {
                toggleAction.action.performed -= OnToggle;
            }

            Close();
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            Toggle();
        }

        private void Open()
        {
            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null || panelTemplate == null)
            {
                return;
            }

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            _panel = panelTemplate.Instantiate().Q(RootName);
            _panel.Q<Button>("close").clicked += Close;
            var content = _panel.Q<ScrollView>("content");
            foreach (var shortcut in shortcuts)
            {
                var row = new VisualElement();
                row.AddToClassList("shortcuts__row");
                var key = new Label(shortcut.KeyText());
                key.AddToClassList("shortcuts__key");
                var text = new Label(shortcut.Description);
                text.AddToClassList("shortcuts__text");
                row.Add(key);
                row.Add(text);
                content.Add(row);
                _keys.Add(key);
            }

            root.Add(_panel);
        }
    }

    [Serializable]
    public struct Shortcut
    {
        [Tooltip("The action whose key is shown; empty for a key handled outside the input actions.")]
        [SerializeField] private InputActionReference action;
        [Tooltip("Shown when there is no action, e.g. F2.")]
        [SerializeField] private string key;
        [SerializeField] private string description;

        public Shortcut(InputActionReference action, string key, string description)
        {
            this.action = action;
            this.key = key;
            this.description = description;
        }

        public string Description => description;

        // The action's current keyboard binding, else the written key.
        public string KeyText()
        {
            if (action != null && action.action != null)
            {
                var shown = action.action.GetBindingDisplayString();
                if (!string.IsNullOrEmpty(shown))
                {
                    return shown;
                }
            }

            return key ?? string.Empty;
        }
    }
}
