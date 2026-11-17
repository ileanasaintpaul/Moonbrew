using UnityEngine;

namespace Moonbrew.Inventory
{
    // One per inventory: the player's bag, the café stock.
    [CreateAssetMenu(menuName = "Moonbrew/Inventory/Inventory Config", fileName = "SO_Inventory_")]
    public class InventoryConfig : ScriptableObject
    {
        [Tooltip("Stable ID carried by InventoryChanged and stored in saves, e.g. inventory_player.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int slotCount = 24;

        public string Id => id;

        public string DisplayName => displayName;

        public int SlotCount => slotCount;
    }
}
