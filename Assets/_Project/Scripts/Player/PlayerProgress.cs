using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Player
{
    // Turns the day's activities into experience, levels the owner up and hands out each level's reward:
    // items go straight to her bag, money and recipes are announced for Economy and Kitchen.
    public class PlayerProgress : MonoBehaviour
    {
        [SerializeField] private ProgressionData data;
        [SerializeField] private InventoryHolder bag;
        [SerializeField] private EventBusAsset eventBus;

        private bool _subscribed;

        public Progression Progression { get; private set; }

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(ProgressionData progressionData, InventoryHolder playerBag, EventBusAsset bus)
        {
            data = progressionData;
            bag = playerBag;
            eventBus = bus;
        }

        private void Awake()
        {
            if (data == null || eventBus == null)
            {
                Debug.LogError($"{nameof(PlayerProgress)} on {name} needs the progression data and the event bus.", this);
                enabled = false;
                return;
            }

            Progression = new Progression(data.XpToNext, data.SkillPointsPerLevel);
        }

        private void OnEnable()
        {
            if (_subscribed || Progression == null)
            {
                return;
            }

            eventBus.Subscribe<OrderServed>(OnOrderServed);
            eventBus.Subscribe<CropHarvested>(OnCropHarvested);
            eventBus.Subscribe<ItemCrafted>(OnItemCrafted);
            eventBus.Subscribe<NewGameStarted>(OnNewGame);
            eventBus.Subscribe<GameLoaded>(OnLoaded);
            eventBus.Subscribe<GameSaving>(OnSaving);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<OrderServed>(OnOrderServed);
            eventBus.Unsubscribe<CropHarvested>(OnCropHarvested);
            eventBus.Unsubscribe<ItemCrafted>(OnItemCrafted);
            eventBus.Unsubscribe<NewGameStarted>(OnNewGame);
            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
            eventBus.Unsubscribe<GameSaving>(OnSaving);
            _subscribed = false;
        }

        // Only what the owner does herself: the employee's tables bring her nothing.
        private void OnOrderServed(OrderServed message)
        {
            if (!message.ByEmployee)
            {
                Gain(data.TableServedXp);
            }
        }

        private void OnCropHarvested(CropHarvested message)
        {
            Gain(data.HarvestXp);
        }

        private void OnItemCrafted(ItemCrafted message)
        {
            if (Progression.FirstCraft(message.ItemId))
            {
                Gain(data.FirstCraftXp);
            }
        }

        private void Gain(int xp)
        {
            foreach (var level in Progression.AddXp(xp))
            {
                Reward(level);
            }

            Announce();
        }

        private void Reward(int level)
        {
            var reward = data.RewardFor(level);
            if (reward.Item != null && reward.Count > 0 && bag != null)
            {
                bag.Inventory.Add(reward.Item, reward.Count);
            }

            Debug.Log($"[Progress] Level {level}");
            eventBus.Publish(new LevelReached(level, data.SkillPointsPerLevel, reward.Money, reward.RecipeId));
        }

        private void OnNewGame(NewGameStarted message)
        {
            Restore(message.Data);
        }

        private void OnLoaded(GameLoaded message)
        {
            Restore(message.Data);
        }

        private void Restore(SaveData save)
        {
            Progression.Restore(save.level, save.xp, save.skillPoints, save.craftedItems);
            Announce();
        }

        private void OnSaving(GameSaving message)
        {
            message.Data.level = Progression.Level;
            message.Data.xp = Progression.Xp;
            message.Data.skillPoints = Progression.SkillPoints;
            message.Data.craftedItems = new List<string>(Progression.Crafted);
        }

        private void Announce()
        {
            eventBus.Publish(new ExperienceChanged(Progression.Level, Progression.Xp, Progression.XpToNext));
        }
    }
}
