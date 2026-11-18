using UnityEngine;

namespace Moonbrew.Cafe
{
    // When and how customers come to the café, and how long they stay.
    [CreateAssetMenu(menuName = "Moonbrew/Cafe/Cafe Config", fileName = "SO_CafeConfig")]
    public class CafeConfig : ScriptableObject
    {
        private const int MinutesPerHour = 60;

        [SerializeField] private CustomerCatalog customers;

        [Tooltip("In-game hours between which groups arrive.")]
        [SerializeField, Min(0)] private int openHour = 11;
        [SerializeField, Min(0)] private int closeHour = 18;

        [Tooltip("Game minutes between two arrivals, picked at random in this range.")]
        [SerializeField, Min(1)] private int arrivalMinMinutes = 30;
        [SerializeField, Min(1)] private int arrivalMaxMinutes = 60;

        [Tooltip("Passing customers come in groups of 1 to this many; villagers come alone.")]
        [SerializeField, Min(1)] private int maxGroupSize = 3;
        [Tooltip("Chance that an arrival is a villager rather than passing customers.")]
        [SerializeField, Range(0f, 1f)] private float villagerChance = 0.3f;

        [Tooltip("Game minutes a seated table takes to choose, picked at random in this range.")]
        [SerializeField, Min(0)] private int chooseMinMinutes = 10;
        [SerializeField, Min(0)] private int chooseMaxMinutes = 20;
        [Tooltip("Game minutes a table waits to order before leaving.")]
        [SerializeField, Min(1)] private int patienceMinutes = 60;

        [Tooltip("Walking speed of the customers, in meters per second.")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.4f;
        [Tooltip("Half the width of a customer, in meters: walking customers keep this far from each other.")]
        [SerializeField, Min(0.05f)] private float customerRadius = 0.3f;
        [Tooltip("Seconds to slide from the aisle onto the chair, and back.")]
        [SerializeField, Min(0f)] private float sitSeconds = 0.4f;
        [Tooltip("Bubble shown above a table; its child named Icon shows the ready icon.")]
        [SerializeField] private GameObject bubblePrefab;
        [SerializeField] private Sprite readyIcon;
        [Tooltip("Gap between the top of the tallest customer and the bottom of the bubble.")]
        [SerializeField, Min(0f)] private float bubbleGap = 0.15f;

        public CustomerCatalog Customers => customers;

        public int OpenMinute => openHour * MinutesPerHour;

        public int CloseMinute => closeHour * MinutesPerHour;

        public int ArrivalMinMinutes => arrivalMinMinutes;

        public int ArrivalMaxMinutes => Mathf.Max(arrivalMinMinutes, arrivalMaxMinutes);

        public int MaxGroupSize => maxGroupSize;

        public float VillagerChance => villagerChance;

        public int ChooseMinMinutes => chooseMinMinutes;

        public int ChooseMaxMinutes => Mathf.Max(chooseMinMinutes, chooseMaxMinutes);

        public int PatienceMinutes => patienceMinutes;

        public float WalkSpeed => walkSpeed;

        public float CustomerRadius => customerRadius;

        public float SitSeconds => sitSeconds;

        public GameObject BubblePrefab => bubblePrefab;

        public Sprite ReadyIcon => readyIcon;

        public float BubbleGap => bubbleGap;

        // For configs built from code; times in game minutes.
        public void Init(CustomerCatalog catalog, int openHours, int closeHours, int arrivalMin, int arrivalMax, int groupSize,
            float villagers, int chooseMin, int chooseMax, int patience)
        {
            customers = catalog;
            openHour = openHours;
            closeHour = closeHours;
            arrivalMinMinutes = arrivalMin;
            arrivalMaxMinutes = arrivalMax;
            maxGroupSize = groupSize;
            villagerChance = villagers;
            chooseMinMinutes = chooseMin;
            chooseMaxMinutes = chooseMax;
            patienceMinutes = patience;
        }

        // For configs built from code: what the scene shows.
        public void InitVisuals(float speed, GameObject bubble, Sprite ready)
        {
            walkSpeed = speed;
            bubblePrefab = bubble;
            readyIcon = ready;
        }
    }
}
