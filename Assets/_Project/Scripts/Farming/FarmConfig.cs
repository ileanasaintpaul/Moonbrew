using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Farming
{
    [CreateAssetMenu(menuName = "Moonbrew/Farming/Farm Config", fileName = "SO_FarmConfig")]
    public class FarmConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float cellSize = 1f;
        [Tooltip("How many cells around the player she can use a tool or plant on.")]
        [SerializeField, Min(0)] private int reach = 1;
        [Tooltip("The item that tills the soil.")]
        [SerializeField] private ItemData hoe;
        [Tooltip("The item that cuts grass.")]
        [SerializeField] private ItemData sickle;
        [SerializeField, Min(0.1f)] private float sickleRadius = 1.5f;
        [Tooltip("Width of the sickle swing in degrees, centered on where the player clicks.")]
        [SerializeField, Range(1f, 360f)] private float sickleArc = 180f;
        [Tooltip("The item that waters the soil.")]
        [SerializeField] private ItemData wateringCan;
        [Tooltip("Waterings in a full can.")]
        [SerializeField, Min(1)] private int canCapacity = 20;
        [Tooltip("A click this close to the well, with the player near it, fills the can.")]
        [SerializeField, Min(0.1f)] private float wellClickRadius = 1.5f;
        [SerializeField, Min(0.1f)] private float wellReach = 2.5f;
        [Tooltip("Multiplies the tilled soil's color while it is watered.")]
        [SerializeField] private Color wetSoilTint = new Color(0.55f, 0.45f, 0.45f, 1f);
        [Tooltip("Bubble shown above a ripe crop: a child named Icon shows the harvest. Clicking it picks the crop.")]
        [SerializeField] private GameObject ripeBubblePrefab;
        [Tooltip("Gap between the top of the ripe plant and the bottom of its bubble.")]
        [SerializeField, Min(0f)] private float ripeBubbleGap = 0.1f;
        [Tooltip("Model of a tilled cell, centered on the cell.")]
        [SerializeField] private GameObject tilledSoilPrefab;
        [Tooltip("Height of the obstacle check above the ground: anything solid in that box stops tilling. It starts above the ground so the ground itself does not count.")]
        [SerializeField, Min(0.01f)] private float obstacleCheckHeight = 1f;
        [Tooltip("Gap kept above the ground so the ground itself is not an obstacle. Small enough to catch flat paths.")]
        [SerializeField, Min(0f)] private float obstacleCheckClearance = 0.02f;

        public float CellSize => cellSize;

        public int Reach => reach;

        public ItemData Hoe => hoe;

        public ItemData Sickle => sickle;

        public float SickleRadius => sickleRadius;

        public float SickleArc => sickleArc;

        public ItemData WateringCan => wateringCan;

        public int CanCapacity => canCapacity;

        public float WellClickRadius => wellClickRadius;

        public float WellReach => wellReach;

        public Color WetSoilTint => wetSoilTint;

        public GameObject RipeBubblePrefab => ripeBubblePrefab;

        public float RipeBubbleGap => ripeBubbleGap;

        public GameObject TilledSoilPrefab => tilledSoilPrefab;

        public float ObstacleCheckHeight => obstacleCheckHeight;

        public float ObstacleCheckClearance => obstacleCheckClearance;

        // For configs built from code.
        public void Init(ItemData hoeItem, ItemData sickleItem, GameObject soilPrefab, ItemData canItem = null, int capacity = 20, GameObject bubblePrefab = null)
        {
            ripeBubblePrefab = bubblePrefab;
            hoe = hoeItem;
            sickle = sickleItem;
            tilledSoilPrefab = soilPrefab;
            wateringCan = canItem;
            canCapacity = capacity;
        }
    }
}
