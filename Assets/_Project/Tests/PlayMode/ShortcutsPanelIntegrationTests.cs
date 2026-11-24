using System.Collections;
using System.Collections.Generic;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // The P list of debug shortcuts: opens and closes, one row per shortcut, keys read from the input actions.
    public class ShortcutsPanelIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private InputAction _pause;
        private InputAction _unbound;

        [SetUp]
        public void SetUp()
        {
            var asset = Create(ScriptableObject.CreateInstance<InputActionAsset>());
            var map = asset.AddActionMap("Debug");
            _pause = map.AddAction("PauseClock", InputActionType.Button, "<Keyboard>/f5");
            _unbound = map.AddAction("Unbound", InputActionType.Button);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator Toggle_Twice_OpensTheListThenClosesIt()
        {
            var panel = CreatePanel(new[]
            {
                new Shortcut(Create(InputActionReference.Create(_pause)), null, "Pause the clock"),
                new Shortcut(null, "F2", "Sharp or pixel image"),
            });
            yield return null;

            panel.Toggle();

            Assert.IsTrue(panel.IsOpen);
            Assert.AreEqual(2, panel.RowCount);
            Assert.AreEqual("F5", panel.KeyText(0), "The key comes from the action's binding.");
            Assert.AreEqual("F2", panel.KeyText(1));

            panel.Toggle();

            Assert.IsFalse(panel.IsOpen);
        }

        [UnityTest]
        public IEnumerator Toggle_NoShortcuts_OpensAnEmptyList()
        {
            var panel = CreatePanel(null);
            yield return null;

            panel.Toggle();

            Assert.IsTrue(panel.IsOpen);
            Assert.AreEqual(0, panel.RowCount);
            panel.Close();
            panel.Close();
            Assert.IsFalse(panel.IsOpen, "Closing twice is harmless.");
        }

        [Test]
        public void KeyText_NoActionNorKey_IsEmpty()
        {
            Assert.AreEqual(string.Empty, new Shortcut(null, null, "Nothing").KeyText());
        }

        [Test]
        public void KeyText_ActionWithoutBinding_FallsBackToTheWrittenKey()
        {
            var reference = Create(InputActionReference.Create(_unbound));

            Assert.AreEqual("P", new Shortcut(reference, "P", "List").KeyText());
        }

        private ShortcutsPanel CreatePanel(Shortcut[] lines)
        {
            var panelObject = Create(new GameObject("Shortcuts"));
            panelObject.SetActive(false);
            var document = panelObject.AddComponent<UIDocument>();
            document.panelSettings = Create(ScriptableObject.CreateInstance<PanelSettings>());
            var panel = panelObject.AddComponent<ShortcutsPanel>();
#if UNITY_EDITOR
            panel.Init(UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/ShortcutsPanel.uxml"), lines);
#else
            Assert.Ignore("The panel template is loaded from the editor asset database.");
#endif
            panelObject.SetActive(true);
            return panel;
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
