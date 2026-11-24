using System.Collections.Generic;
using Moonbrew.Inventory;
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

        [Tooltip("Turns the dish IDs of the menu back into items, for tastes and icons.")]
        [SerializeField] private ItemCatalog items;
        [Tooltip("How close to a table the owner must stand to take its order, in meters.")]
        [SerializeField, Min(0.1f)] private float orderReach = 2.5f;
        [Tooltip("Height of an order bubble's tip above the customer's head bone, in meters.")]
        [SerializeField, Min(0f)] private float orderBubbleAboveHead = 0.4f;
        [Tooltip("How close to the fridge the owner must stand to open it, in meters.")]
        [SerializeField, Min(0.1f)] private float fridgeReach = 2f;
        [Tooltip("Seats of each table of the café, in the order of CafeFloor: the café runs from scenes without its tables.")]
        [SerializeField] private int[] tableSeats = new int[0];
        [Tooltip("Extra pay, in percent, on a table served by the owner rather than the employee: the income gap to balance.")]
        [SerializeField, Min(0)] private int ownerBonusPercent;
        [Tooltip("Game minutes a served table eats before going to pay at the till.")]
        [SerializeField, Min(0)] private int eatMinutes = 20;
        [Tooltip("Icon in the bubble of customers waiting at the till.")]
        [SerializeField] private Sprite payIcon;
        [Tooltip("Gap between two customers queuing at the till, in meters.")]
        [SerializeField, Min(0.1f)] private float queueSpacing = 0.7f;
        [Tooltip("How close to the till the owner must stand to cash a bill, in meters.")]
        [SerializeField, Min(0.1f)] private float tillReach = 2.5f;

        public IReadOnlyList<int> TableSeats => tableSeats;

        public int OwnerBonusPercent => ownerBonusPercent;

        public int EatMinutes => eatMinutes;

        public Sprite PayIcon => payIcon;

        public float QueueSpacing => queueSpacing;

        public float TillReach => tillReach;

        public float FridgeReach => fridgeReach;

        public CustomerCatalog Customers => customers;

        public ItemCatalog Items => items;

        public float OrderReach => orderReach;

        public float OrderBubbleAboveHead => orderBubbleAboveHead;

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

        // For configs built from code: what taking an order needs.
        public void InitOrders(ItemCatalog catalog, float reach)
        {
            items = catalog;
            orderReach = reach;
        }

        // For configs built from code: the café's tables.
        public void InitTables(int[] seats)
        {
            tableSeats = seats;
        }

        // For configs built from code.
        public void InitOwnerBonus(int percent)
        {
            ownerBonusPercent = percent;
        }

        // For configs built from code: the till.
        public void InitTill(int eat, Sprite pay, float reach)
        {
            eatMinutes = eat;
            payIcon = pay;
            tillReach = reach;
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
