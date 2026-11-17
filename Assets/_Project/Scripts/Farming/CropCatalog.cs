using System.Collections.Generic;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Farming
{
    // Every crop of the game: finds a crop by its saved ID, or the crop a seed plants.
    [CreateAssetMenu(menuName = "Moonbrew/Farming/Crop Catalog", fileName = "SO_CropCatalog")]
    public class CropCatalog : ScriptableObject
    {
        [SerializeField] private CropData[] crops = new CropData[0];

        public IReadOnlyList<CropData> Crops => crops;

        // For catalogs built from code.
        public void Init(CropData[] catalogCrops)
        {
            crops = catalogCrops ?? new CropData[0];
        }

        public CropData Find(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var crop in crops)
            {
                if (crop != null && crop.Id == id)
                {
                    return crop;
                }
            }

            return null;
        }

        // Null when the item is not a seed of any crop.
        public CropData ForSeed(ItemData seed)
        {
            if (seed == null)
            {
                return null;
            }

            foreach (var crop in crops)
            {
                if (crop != null && crop.Seed == seed)
                {
                    return crop;
                }
            }

            return null;
        }
    }
}
