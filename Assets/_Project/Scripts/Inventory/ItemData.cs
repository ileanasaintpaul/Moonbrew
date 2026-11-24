using UnityEngine;

namespace Moonbrew.Inventory
{
    [CreateAssetMenu(menuName = "Moonbrew/Inventory/Item", fileName = "SO_Item_")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("Stable snake_case ID stored in saves, e.g. crop_wheat. Never change it once shipped.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private ItemCategory category;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0)] private int basePrice;
        [SerializeField, Min(1)] private int maxStack = 99;
        [Tooltip("The model shown when the item is set out, e.g. a slice in the display case. Optional.")]
        [SerializeField] private GameObject worldModel;

        public string Id => id;

        public string DisplayName => displayName;

        public ItemCategory Category => category;

        public Sprite Icon => icon;

        public int BasePrice => basePrice;

        public int MaxStack => maxStack;

        public GameObject WorldModel => worldModel;
    }
}
