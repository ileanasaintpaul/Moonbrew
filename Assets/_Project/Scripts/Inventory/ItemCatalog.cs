using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Inventory
{
    // Every item of the game, so a save can turn stored IDs back into items.
    [CreateAssetMenu(menuName = "Moonbrew/Inventory/Item Catalog", fileName = "SO_ItemCatalog")]
    public class ItemCatalog : ScriptableObject
    {
        [SerializeField] private ItemData[] items = new ItemData[0];

        private Dictionary<string, ItemData> _byId;

        public IReadOnlyList<ItemData> Items => items;

        // For catalogs built from code.
        public void Init(ItemData[] catalogItems)
        {
            items = catalogItems ?? new ItemData[0];
            _byId = null;
        }

        public ItemData Find(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (_byId == null)
            {
                _byId = new Dictionary<string, ItemData>();
                foreach (var item in items)
                {
                    if (item != null && !string.IsNullOrEmpty(item.Id))
                    {
                        _byId[item.Id] = item;
                    }
                }
            }

            return _byId.TryGetValue(id, out var found) ? found : null;
        }

        // The cache follows edits made in the inspector.
        private void OnValidate()
        {
            _byId = null;
        }
    }
}
