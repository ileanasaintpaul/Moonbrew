using System;
using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Economy;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.PlayMode
{
    // Tables pay into the wallet, the save keeps it and the HUD shows it.
    public class EconomyIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private readonly List<MoneyChanged> _changes = new List<MoneyChanged>();
        private EventBusAsset _bus;
        private WalletHolder _holder;
        private MoneyHud _hud;
        private Action<MoneyChanged> _onChanged;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            var panel = Create(ScriptableObject.CreateInstance<PanelSettings>());
            var hudObject = Create(new GameObject("HUD"));
            hudObject.SetActive(false);
            hudObject.AddComponent<UIDocument>().panelSettings = panel;
            _hud = hudObject.AddComponent<MoneyHud>();
            hudObject.SetActive(true);
            _hud.Init(_bus);

            var walletObject = Create(new GameObject("Wallet"));
            walletObject.SetActive(false);
            _holder = walletObject.AddComponent<WalletHolder>();
            _holder.Init(_bus);
            walletObject.SetActive(true);

            _onChanged = _changes.Add;
            _bus.Subscribe(_onChanged);
        }

        [TearDown]
        public void TearDown()
        {
            _bus.Unsubscribe(_onChanged);
            _changes.Clear();
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
        public IEnumerator BillPaid_AddsThePaymentAndShowsIt()
        {
            yield return null;
            _bus.Publish(new GameLoaded(SaveWith(20)));

            _bus.Publish(new BillPaid(1, new[] { "dish_cake" }, 15));

            Assert.AreEqual(35, _holder.Wallet.Balance);
            Assert.AreEqual("35", _hud.AmountText);
            Assert.AreEqual(15, _changes[_changes.Count - 1].Delta);
        }

        [UnityTest]
        public IEnumerator BillPaid_NoPayment_ChangesNothing()
        {
            yield return null;

            _bus.Publish(new BillPaid(1, Array.Empty<string>(), 0));

            Assert.AreEqual(0, _holder.Wallet.Balance);
            Assert.IsEmpty(_changes);
        }

        [UnityTest]
        public IEnumerator GameLoaded_ShowsTheSavedBalance()
        {
            yield return null;

            _bus.Publish(new GameLoaded(SaveWith(42)));

            Assert.AreEqual("42", _hud.AmountText);
        }

        [UnityTest]
        public IEnumerator NewGameStarted_ShowsAnEmptyWallet()
        {
            yield return null;

            _bus.Publish(new NewGameStarted(SaveData.CreateNew("slot_new", 1)));

            Assert.AreEqual("0", _hud.AmountText);
        }

        [UnityTest]
        public IEnumerator GameSaving_WritesTheBalance()
        {
            yield return null;
            _bus.Publish(new BillPaid(0, new[] { "dish_cake" }, 12));
            var save = SaveData.CreateNew("slot_a", 1);

            _bus.Publish(new GameSaving(save));

            Assert.AreEqual(12, save.money);
        }

        [UnityTest]
        public IEnumerator Spend_Affordable_LowersTheBalanceShown()
        {
            yield return null;
            _bus.Publish(new GameLoaded(SaveWith(10)));

            Assert.IsTrue(_holder.Spend(4));

            Assert.AreEqual("6", _hud.AmountText);
            Assert.AreEqual(-4, _changes[_changes.Count - 1].Delta);
        }

        [UnityTest]
        public IEnumerator Spend_TooMuch_IsRefusedWithoutAnnouncing()
        {
            yield return null;
            _bus.Publish(new GameLoaded(SaveWith(3)));
            _changes.Clear();

            Assert.IsFalse(_holder.Spend(4));

            Assert.AreEqual("3", _hud.AmountText);
            Assert.IsEmpty(_changes);
        }

        private static SaveData SaveWith(int money)
        {
            var save = SaveData.CreateNew("slot_a", 1);
            save.money = money;
            return save;
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
