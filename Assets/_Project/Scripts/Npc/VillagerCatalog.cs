using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Npc
{
    [CreateAssetMenu(menuName = "Moonbrew/Npc/Villager Catalog", fileName = "SO_VillagerCatalog")]
    public class VillagerCatalog : ScriptableObject
    {
        [SerializeField] private VillagerData[] villagers = new VillagerData[0];

        public IReadOnlyList<VillagerData> Villagers => villagers;

        public VillagerData Find(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var villager in villagers)
            {
                if (villager != null && villager.Id == id)
                {
                    return villager;
                }
            }

            return null;
        }

        // For catalogs built from code.
        public void Init(VillagerData[] catalogVillagers)
        {
            villagers = catalogVillagers ?? new VillagerData[0];
        }
    }
}
