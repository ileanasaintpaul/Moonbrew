using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Npc;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // A villager's order opens their conversation, which pauses the game until its last line;
    // serving them herself brings the owner closer to them.
    public class DialogueIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private readonly List<bool> _menus = new List<bool>();
        private EventBusAsset _bus;
        private DialoguePanel _panel;
        private RelationBook _relations;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _bus.Subscribe<GameMenuToggled>(message => _menus.Add(message.Open));
            var rose = Create(ScriptableObject.CreateInstance<VillagerData>());
            rose.Init("villager_rose", "Rose", new[]
            {
                new DialogueLine(Speaker.Villager, "Hello {owner}! A {dish}, please.", "happy", "wave"),
                new DialogueLine(Speaker.Owner, "Coming up.", "listening", "idle"),
            }, 2);
            var villagers = Create(ScriptableObject.CreateInstance<VillagerCatalog>());
            villagers.Init(new[] { rose });
            var items = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            items.Init(new ItemData[0]);

            var panelSettings = Create(ScriptableObject.CreateInstance<PanelSettings>());
            var hud = Create(new GameObject("HUD"));
            hud.SetActive(false);
            hud.AddComponent<UIDocument>().panelSettings = panelSettings;
            _panel = hud.AddComponent<DialoguePanel>();
            hud.SetActive(true);
            _panel.Init(_bus, villagers, items);

            var book = Create(new GameObject("Relations"));
            book.SetActive(false);
            _relations = book.AddComponent<RelationBook>();
            _relations.Init(villagers, _bus);
            book.SetActive(true);
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
            _menus.Clear();
        }

        [UnityTest]
        public IEnumerator VillagerOrders_OpensTheConversationAndPauses()
        {
            yield return null;
            _bus.Publish(new GameLoaded(OwnerSave("Ana")));

            _bus.Publish(new OrderTaken(1, new[] { "dish_cake" }, new[] { "villager_rose" }));
            _panel.Advance();

            Assert.IsTrue(_panel.IsOpen);
            StringAssert.EndsWith("Rose", _panel.SpeakerName);
            Assert.AreEqual("Hello Ana! A dish_cake, please.", _panel.LineText);
            CollectionAssert.AreEqual(new[] { true }, _menus);
        }

        [UnityTest]
        public IEnumerator Advance_PastTheLastLine_ClosesAndResumes()
        {
            yield return null;
            _bus.Publish(new OrderTaken(1, new[] { "dish_cake" }, new[] { "villager_rose" }));

            _panel.Advance();
            _panel.Advance();
            Assert.AreEqual("Coming up.", _panel.LineText);
            _panel.Advance();
            _panel.Advance();

            Assert.IsFalse(_panel.IsOpen);
            CollectionAssert.AreEqual(new[] { true, false }, _menus);
        }

        [UnityTest]
        public IEnumerator OrderRestoredOrByPassingCustomers_OpensNothing()
        {
            yield return null;

            _bus.Publish(new OrderTaken(1, new[] { "dish_cake" }, new[] { "villager_rose" }, true));
            _bus.Publish(new OrderTaken(2, new[] { "dish_cake" }, new[] { "customer_knight" }));

            Assert.IsFalse(_panel.IsOpen);
        }

        [UnityTest]
        public IEnumerator ServedByTheOwner_RaisesTheRelationAndSavesIt()
        {
            yield return null;
            var changes = new List<RelationChanged>();
            _bus.Subscribe<RelationChanged>(changes.Add);

            _bus.Publish(new OrderServed(1, new[] { "dish_cake" }, 10, false, new[] { "villager_rose" }));
            _bus.Publish(new OrderServed(1, new[] { "dish_cake" }, 10, true, new[] { "villager_rose" }));
            var save = SaveData.CreateNew("slot_a", 1);
            _bus.Publish(new GameSaving(save));

            Assert.AreEqual(1, changes.Count, "The employee's service brings no friendship.");
            Assert.AreEqual((2, 2), (changes[0].Points, changes[0].Delta));
            Assert.AreEqual(("villager_rose", 2), (save.relations[0].villagerId, save.relations[0].points));
        }

        private static SaveData OwnerSave(string ownerName)
        {
            var save = SaveData.CreateNew("slot_a", 1);
            save.ownerName = ownerName;
            return save;
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
