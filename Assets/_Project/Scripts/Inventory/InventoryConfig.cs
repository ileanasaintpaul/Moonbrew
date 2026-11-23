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
        [Tooltip("How close the owner must stand to drop ingredients here, in meters, for a stock with a drop point.")]
        [SerializeField, Min(0.1f)] private float dropReach = 2.5f;

        public float DropReach => dropReach;

        public string Id => id;

        public string DisplayName => displayName;

        public int SlotCount => slotCount;
    }
}
