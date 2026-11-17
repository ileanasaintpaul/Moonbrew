using System;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Farming
{
    // A crop: which seed plants it, what it gives, how long it grows and what each growth stage looks like.
    [CreateAssetMenu(menuName = "Moonbrew/Farming/Crop", fileName = "SO_Crop_")]
    public class CropData : ScriptableObject
    {
        [Tooltip("Stable snake_case ID stored in saves, e.g. wheat. Never change it once shipped.")]
        [SerializeField] private string id;
        [SerializeField] private ItemData seed;
        [SerializeField] private ItemData harvest;
        [Tooltip("Days of growth from planting to the first harvest.")]
        [SerializeField, Min(1)] private int growthDays = 4;
        [Tooltip("Days for the fruit to grow back after a harvest. 0: the plant is gone once harvested.")]
        [SerializeField, Min(0)] private int regrowDays;
        [SerializeField, Min(1)] private int harvestMin = 1;
        [SerializeField, Min(1)] private int harvestMax = 1;
        [Tooltip("One model per growth stage, from the sprout to the ripe plant (last). A regrowing plant shows the stage before the last.")]
        [SerializeField] private GameObject[] stagePrefabs = new GameObject[0];

        public string Id => id;

        public ItemData Seed => seed;

        public ItemData Harvest => harvest;

        public int GrowthDays => growthDays;

        public int RegrowDays => regrowDays;

        public bool Regrows => regrowDays > 0;

        public int HarvestMin => harvestMin;

        public int HarvestMax => harvestMax;

        public int StageCount => stagePrefabs.Length;

        public GameObject StagePrefab(int stage)
        {
            return stage >= 0 && stage < stagePrefabs.Length ? stagePrefabs[stage] : null;
        }

        // The stage shown after a number of days of growth: the stages before the last are spread over
        // the growth days, and the last (ripe) stage is reached on the final day.
        public int StageForDay(int daysGrown)
        {
            if (stagePrefabs.Length == 0)
            {
                throw new InvalidOperationException($"Crop '{id}' has no growth stages.");
            }

            var ripe = stagePrefabs.Length - 1;
            if (daysGrown <= 0)
            {
                return 0;
            }

            if (daysGrown >= growthDays)
            {
                return ripe;
            }

            return Math.Min(ripe - 1, daysGrown * ripe / growthDays);
        }
    }
}
