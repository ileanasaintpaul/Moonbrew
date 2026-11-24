using System;
using System.Collections.Generic;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Player
{
    // How the owner levels up: the experience each activity gives, the curve and what each level brings.
    [CreateAssetMenu(menuName = "Moonbrew/Player/Progression", fileName = "SO_Progression")]
    public class ProgressionData : ScriptableObject
    {
        [Tooltip("Experience for each table the owner serves herself.")]
        [SerializeField, Min(0)] private int tableServedXp = 10;
        [Tooltip("Experience for each crop picked.")]
        [SerializeField, Min(0)] private int harvestXp = 4;
        [Tooltip("Experience the first time each dish or ingredient is made.")]
        [SerializeField, Min(0)] private int firstCraftXp = 15;
        [Tooltip("Experience needed to go from each level to the next; the last value repeats.")]
        [SerializeField] private int[] xpToNext = { 50, 80, 120, 170, 230, 300 };
        [SerializeField, Min(0)] private int skillPointsPerLevel = 1;
        [SerializeField] private LevelReward[] rewards = new LevelReward[0];

        public int TableServedXp => tableServedXp;

        public int HarvestXp => harvestXp;

        public int FirstCraftXp => firstCraftXp;

        public IReadOnlyList<int> XpToNext => xpToNext;

        public int SkillPointsPerLevel => skillPointsPerLevel;

        public IReadOnlyList<LevelReward> Rewards => rewards;

        public LevelReward RewardFor(int level)
        {
            foreach (var reward in rewards)
            {
                if (reward.Level == level)
                {
                    return reward;
                }
            }

            return default;
        }

        // For data built from code.
        public void Init(int[] curve, int pointsPerLevel, LevelReward[] levelRewards, int servedXp = 10, int pickedXp = 4, int craftedXp = 15)
        {
            xpToNext = curve;
            skillPointsPerLevel = pointsPerLevel;
            rewards = levelRewards ?? new LevelReward[0];
            tableServedXp = servedXp;
            harvestXp = pickedXp;
            firstCraftXp = craftedXp;
        }
    }

    [Serializable]
    public struct LevelReward
    {
        [SerializeField, Min(2)] private int level;
        [SerializeField, Min(0)] private int money;
        [Tooltip("ID of a recipe learned at this level, or empty.")]
        [SerializeField] private string recipeId;
        [SerializeField] private ItemData item;
        [SerializeField, Min(0)] private int count;

        public LevelReward(int level, int money, string recipeId, ItemData item, int count)
        {
            this.level = level;
            this.money = money;
            this.recipeId = recipeId;
            this.item = item;
            this.count = count;
        }

        public int Level => level;

        public int Money => money;

        public string RecipeId => recipeId;

        public ItemData Item => item;

        public int Count => count;
    }
}
