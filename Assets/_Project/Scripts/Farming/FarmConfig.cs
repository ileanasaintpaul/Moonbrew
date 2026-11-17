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

        public GameObject TilledSoilPrefab => tilledSoilPrefab;

        public float ObstacleCheckHeight => obstacleCheckHeight;

        public float ObstacleCheckClearance => obstacleCheckClearance;

        // For configs built from code.
        public void Init(ItemData hoeItem, ItemData sickleItem, GameObject soilPrefab)
        {
            hoe = hoeItem;
            sickle = sickleItem;
            tilledSoilPrefab = soilPrefab;
        }
    }
}
