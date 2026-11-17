using Moonbrew.Core;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Kitchen
{
    // A processing station in the scene, like the mill: a click with an input item in hand loads it,
    // game time runs the batch (finishing overnight), and a click on a ready station collects the output.
    public class Station : MonoBehaviour
    {
        [SerializeField] private StationData data;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private InventoryHolder bag;
        [SerializeField] private Transform player;
        private const string BubbleIconName = "Icon";

        private bool _subscribed;
        private int _selectedSlot;
        private int _lastMinutes = -1;

        private GameObject _bubble;
        private SpriteRenderer _bubbleIcon;

        public ProcessingStation State { get; private set; }

        public bool BubbleVisible => _bubble != null && _bubble.activeSelf;

        // The icon in the bubble, or null when the bubble is hidden (idle station).
        public Sprite BubbleIcon => _bubble != null && _bubble.activeSelf && _bubbleIcon != null ? _bubbleIcon.sprite : null;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(StationData stationData, EventBusAsset bus, InventoryHolder playerBag, Transform playerTransform)
        {
            data = stationData;
            eventBus = bus;
            bag = playerBag;
            player = playerTransform;
        }

        // Loads the item in hand, or collects a finished batch. Returns true when something happened.
        public bool Use()
        {
            if (State.IsReady)
            {
                return TryCollect();
            }

            var item = ItemInHand();
            var process = data.ProcessFor(item);
            var batches = State.Load(process, bag.Inventory.Count(item));
            if (batches == 0)
            {
                return false;
            }

            bag.Inventory.Remove(item, batches * process.InputQuantity);
            Debug.Log($"[Station] {data.Id}: {batches} batch(es) of {process.Id} started");
            ShowReady();
            return true;
        }

        private void Awake()
        {
            if (data == null)
            {
                Debug.LogError($"{nameof(Station)} on {name} has no {nameof(StationData)} assigned.", this);
                enabled = false;
                return;
            }

            State = new ProcessingStation(data.Capacity);
            if (data.BubblePrefab != null)
            {
                var position = transform.position + Vector3.up * data.BubbleHeight;
                _bubble = Instantiate(data.BubblePrefab, position, data.BubblePrefab.transform.rotation, transform);
                var icon = _bubble.transform.Find(BubbleIconName);
                _bubbleIcon = icon != null ? icon.GetComponent<SpriteRenderer>() : null;
                _bubble.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null || State == null)
            {
                return;
            }

            eventBus.Subscribe<GroundClicked>(OnGroundClicked);
            eventBus.Subscribe<TargetClicked>(OnTargetClicked);
            eventBus.Subscribe<HotbarSlotSelected>(OnSlotSelected);
            eventBus.Subscribe<TimeChanged>(OnTimeChanged);
            eventBus.Subscribe<NightPassed>(OnNightPassed);
            eventBus.Subscribe<GameLoaded>(OnLoaded);
            eventBus.Subscribe<GameSaving>(OnSaving);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<GroundClicked>(OnGroundClicked);
            eventBus.Unsubscribe<TargetClicked>(OnTargetClicked);
            eventBus.Unsubscribe<HotbarSlotSelected>(OnSlotSelected);
            eventBus.Unsubscribe<TimeChanged>(OnTimeChanged);
            eventBus.Unsubscribe<NightPassed>(OnNightPassed);
            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
            eventBus.Unsubscribe<GameSaving>(OnSaving);
            _subscribed = false;
        }

        private void Start()
        {
            ShowReady();
        }

        private void OnGroundClicked(GroundClicked message)
        {
            if (FlatDistance(message.Point, transform.position) <= data.ClickRadius && PlayerNearby())
            {
                Use();
            }
        }

        // A click on the station's model or on its bubble.
        private void OnTargetClicked(TargetClicked message)
        {
            if (message.Target != null && message.Target.transform.IsChildOf(transform) && PlayerNearby())
            {
                Use();
            }
        }

        private bool PlayerNearby()
        {
            return player != null && FlatDistance(player.position, transform.position) <= data.Reach;
        }

        private void OnSlotSelected(HotbarSlotSelected message)
        {
            _selectedSlot = message.Index;
        }

        // Game time only moves forward within a day; the first time heard is just a starting point.
        private void OnTimeChanged(TimeChanged message)
        {
            if (_lastMinutes >= 0 && message.Minutes > _lastMinutes && State.Advance(message.Minutes - _lastMinutes))
            {
                Debug.Log($"[Station] {data.Id}: ready");
                ShowReady();
            }

            _lastMinutes = message.Minutes;
        }

        // A batch still running at the end of the day is done by morning.
        private void OnNightPassed(NightPassed message)
        {
            State.Advance(State.MinutesLeft);
            _lastMinutes = -1;
        }

        private void OnLoaded(GameLoaded message)
        {
            State.Restore(message.Data.FindStation(data.Id), data.FindProcess);
            ShowReady();
        }

        private void OnSaving(GameSaving message)
        {
            message.Data.stations.Add(State.ToSave(data.Id));
        }

        // Only when the whole output fits in the bag; otherwise it waits in the station.
        private bool TryCollect()
        {
            var output = State.Process.Output;
            var count = State.OutputCount;
            if (bag.Inventory.SpaceFor(output) < count)
            {
                Debug.Log($"[Station] {data.Id}: no room in the bag for {count} {output.Id}");
                return false;
            }

            bag.Inventory.Add(output, count);
            State.Collect();
            ShowReady();
            eventBus.Publish(new ItemCrafted(data.Id, output.Id, count));
            Debug.Log($"[Station] {data.Id}: collected {count} {output.Id}");
            return true;
        }

        private ItemData ItemInHand()
        {
            var slots = bag != null && bag.Inventory != null ? bag.Inventory.Slots : null;
            if (slots == null || _selectedSlot < 0 || _selectedSlot >= slots.Count || slots[_selectedSlot].IsEmpty)
            {
                return null;
            }

            return slots[_selectedSlot].Item;
        }

        // Hourglass while working, the output's icon when ready, nothing when idle.
        private void ShowReady()
        {
            if (_bubble == null)
            {
                return;
            }

            _bubble.SetActive(!State.IsIdle);
            if (_bubbleIcon != null)
            {
                _bubbleIcon.sprite = State.IsReady ? State.Process.Output.Icon : data.WorkingIcon;
            }
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }
    }
}
