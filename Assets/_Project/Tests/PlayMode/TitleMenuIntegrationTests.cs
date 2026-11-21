using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Moonbrew.Core;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.PlayMode
{
    // The title screen opens on the logo; the main menu passes the player's choices on the bus,
    // and GameLauncher turns them into a game to open and the morning scene to load.
    public class TitleMenuIntegrationTests
    {
        private static readonly DateTime Earlier = new DateTime(2026, 11, 19, 20, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Later = new DateTime(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc);

        private readonly List<Object> _created = new List<Object>();
        private readonly List<object> _published = new List<object>();
        private string _folder;
        private EventBusAsset _bus;
        private SaveStore _store;
        private GameSlots _slots;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "moonbrew_tests_" + Guid.NewGuid().ToString("N"));
            _store = new SaveStore(_folder);
            _slots = new GameSlots(_store);
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _bus.Subscribe<ContinueChosen>(message => _published.Add(message));
            _bus.Subscribe<NewGameChosen>(message => _published.Add(message));
            _bus.Subscribe<GameChosen>(message => _published.Add(message));
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
            _published.Clear();
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        [UnityTest]
        public IEnumerator Start_OpensOnTheTitleScreen()
        {
            var menu = CreateMenu();
            yield return null;

            Assert.AreEqual("title-screen", menu.CurrentScreen);
        }

        [UnityTest]
        public IEnumerator OpenMainMenu_NoSavedGame_ContinueIsDisabled()
        {
            var menu = CreateMenu();
            yield return null;

            menu.OpenMainMenu();

            Assert.AreEqual("main-screen", menu.CurrentScreen);
            Assert.IsFalse(menu.ButtonNamed("continue").enabledSelf);
        }

        [UnityTest]
        public IEnumerator ContinueClicked_WithASavedGame_IsPassedOn()
        {
            _slots.CreateNew(1, Earlier);
            var menu = CreateMenu();
            yield return null;

            menu.OpenMainMenu();
            Click(menu.ButtonNamed("continue"));

            Assert.IsTrue(menu.ButtonNamed("continue").enabledSelf);
            Assert.IsInstanceOf<ContinueChosen>(_published[0]);
        }

        [UnityTest]
        public IEnumerator NewGameClicked_IsPassedOn()
        {
            var menu = CreateMenu();
            yield return null;

            menu.OpenMainMenu();
            Click(menu.ButtonNamed("new-game"));

            Assert.IsInstanceOf<NewGameChosen>(_published[0]);
        }

        [UnityTest]
        public IEnumerator LoadGame_TwoGames_ListsTheLatestFirstAndPassesTheChosenOne()
        {
            var older = _slots.CreateNew(1, Earlier);
            _slots.CreateNew(1, Later);
            var menu = CreateMenu();
            yield return null;

            menu.OpenMainMenu();
            Click(menu.ButtonNamed("load"));
            Click(menu.GameRowAt(1));

            Assert.AreEqual("load-screen", menu.CurrentScreen);
            Assert.AreEqual(2, menu.GameRowCount);
            Assert.AreEqual(older.slotId, ((GameChosen)_published[0]).SlotId);
        }

        [UnityTest]
        public IEnumerator Launcher_NewGameChosen_OpensCharacterCreationWithoutCreatingAGame()
        {
            var launcher = CreateLauncher(out var config);
            yield return null;

            _bus.Publish(new NewGameChosen());

            Assert.AreEqual(config.CreationScene, launcher.LoadedScene);
            Assert.IsNull(_store.MostRecent());
        }

        [UnityTest]
        public IEnumerator Launcher_CharacterCreated_CreatesTheGameWithTheOwnerAndLoadsTheMorningScene()
        {
            var launcher = CreateLauncher(out var config);
            yield return null;

            _bus.Publish(new CharacterCreated("Rose", new AppearanceSave { topId = "top_villager_1", hairId = "hair_bob" }));

            var game = _store.MostRecent();
            Assert.AreEqual(config.WakeScene, launcher.LoadedScene);
            Assert.IsTrue(game.isNew);
            Assert.AreEqual("Rose", game.ownerName);
            Assert.AreEqual(("top_villager_1", "hair_bob"), (game.appearance.topId, game.appearance.hairId));
        }

        [UnityTest]
        public IEnumerator Launcher_TitleScreenChosen_GoesBackToTheTitleScreen()
        {
            var launcher = CreateLauncher(out var config);
            yield return null;

            _bus.Publish(new TitleScreenChosen());

            Assert.AreEqual(config.TitleScene, launcher.LoadedScene);
        }

        [UnityTest]
        public IEnumerator Launcher_ContinueChosen_NoGame_StaysOnTheMenu()
        {
            var launcher = CreateLauncher(out _);
            yield return null;

            _bus.Publish(new ContinueChosen());

            Assert.IsNull(launcher.LoadedScene);
        }

        [UnityTest]
        public IEnumerator Launcher_GameChosen_OlderGameBecomesTheOneToOpen()
        {
            var older = _slots.CreateNew(1, Earlier);
            _slots.CreateNew(1, Later);
            var launcher = CreateLauncher(out var config);
            yield return null;

            _bus.Publish(new GameChosen(older.slotId));

            Assert.AreEqual(config.WakeScene, launcher.LoadedScene);
            Assert.AreEqual(older.slotId, _store.MostRecent().slotId);
        }

        [UnityTest]
        public IEnumerator Launcher_GameChosen_MissingGame_LoadsNothing()
        {
            var launcher = CreateLauncher(out _);
            yield return null;

            _bus.Publish(new GameChosen("missing"));

            Assert.IsNull(launcher.LoadedScene);
        }

        private TitleMenu CreateMenu()
        {
            var menuObject = Create(new GameObject("Title Menu"));
            menuObject.SetActive(false);
            var document = menuObject.AddComponent<UIDocument>();
            document.panelSettings = Create(ScriptableObject.CreateInstance<PanelSettings>());
#if UNITY_EDITOR
            document.visualTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/TitleMenu.uxml");
#else
            Assert.Ignore("The menu template is loaded from the editor asset database.");
#endif
            var menu = menuObject.AddComponent<TitleMenu>();
            menu.Init(_bus, _folder);
            menuObject.SetActive(true);
            return menu;
        }

        private GameLauncher CreateLauncher(out SessionConfig config)
        {
            config = Create(ScriptableObject.CreateInstance<SessionConfig>());
            var clock = Create(ScriptableObject.CreateInstance<ClockConfig>());
            var launcherObject = Create(new GameObject("Game Launcher"));
            launcherObject.SetActive(false);
            var launcher = launcherObject.AddComponent<GameLauncher>();
            launcher.Init(config, clock, _bus, _folder, false);
            launcherObject.SetActive(true);
            return launcher;
        }

        private static void Click(Button button)
        {
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
