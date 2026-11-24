using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Farming
{
    // The farm's soil: on a ground click near the player, the hoe tills a free cell, seeds are planted,
    // the can waters (and refills at the well) and the sickle cuts grass. Crops grow overnight. Keeps the
    // scene in step with FarmPlots and saves it.
    public class FarmField : MonoBehaviour
    {
        private const int OverlapBufferSize = 16;
        private const string BubbleIconName = "Icon";

        [SerializeField] private FarmConfig config;
        [SerializeField] private CropCatalog crops;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private InventoryHolder bag;
        [SerializeField] private Transform player;
        [Tooltip("Every grass clump of the farm, filled by the editor setup.")]
        [SerializeField] private CuttableGrass[] grass = new CuttableGrass[0];
        [Tooltip("Where the watering can is filled.")]
        [SerializeField] private Transform well;

        private readonly Collider[] _overlaps = new Collider[OverlapBufferSize];
        private readonly HashSet<string> _cutGrass = new HashSet<string>();
        private readonly Dictionary<Vector2Int, GameObject> _soil = new Dictionary<Vector2Int, GameObject>();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly HashSet<Vector2Int> _wet = new HashSet<Vector2Int>();
        private MaterialPropertyBlock _tint;
        private System.Random _random;
        private readonly Dictionary<Vector2Int, GameObject> _plants = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<Vector2Int, GameObject> _bubbles = new Dictionary<Vector2Int, GameObject>();
        private bool _subscribed;
        private int _selectedSlot;

        public FarmPlots Plots { get; } = new FarmPlots();

        public WateringCan Can { get; private set; }

        public bool HasPlantModel(Vector2Int cell) => _plants.ContainsKey(cell);

        public bool HasSoilModel(Vector2Int cell) => _soil.ContainsKey(cell);

        public bool ShowsWet(Vector2Int cell) => _wet.Contains(cell);

        // The model shown for a planted cell, or null.
        public GameObject PlantModel(Vector2Int cell) => _plants.TryGetValue(cell, out var model) ? model : null;

        public int CutGrassCount => _cutGrass.Count;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(FarmConfig farmConfig, CropCatalog cropCatalog, EventBusAsset bus, InventoryHolder playerBag, Transform playerTransform, CuttableGrass[] grassClumps = null, Transform wellTransform = null)
        {
            config = farmConfig;
            crops = cropCatalog;
            eventBus = bus;
            bag = playerBag;
            player = playerTransform;
            grass = grassClumps ?? new CuttableGrass[0];
            well = wellTransform;
        }

        // Tests pass a seeded source so harvests are predictable.
        public void SetRandom(System.Random random)
        {
            _random = random;
        }

        // Uses the item in hand towards a ground point. Returns true when something happened.
        public bool UseAt(Vector3 point)
        {
            if (player == null)
            {
                return false;
            }

            var cell = CellOf(point);
            var inReach = FarmGrid.InReach(CellOf(player.position), cell, config.Reach);

            // A ripe crop is picked whatever the player holds, even nothing.
            if (inReach && TryHarvest(cell))
            {
                return true;
            }

            var item = ItemInHand();
            if (item == null)
            {
                return false;
            }

            // The sickle swings towards the click, whatever its distance.
            if (item == config.Sickle)
            {
                if (Swing(point - player.position) == 0)
                {
                    return false;
                }

                eventBus.Publish(new ToolUsed(config.SickleStamina));
                return true;
            }

            // Watering a cell comes first, so the soil right next to the well can still be watered.
            if (item == config.WateringCan)
            {
                return (inReach && TryWater(cell)) || (NearWell(point) && RefillCan());
            }

            if (!inReach)
            {
                return false;
            }

            if (item == config.Hoe)
            {
                return TryTill(cell);
            }

            var crop = crops != null ? crops.ForSeed(item) : null;
            return crop != null && TryPlant(cell, crop, item);
        }

        // The grid follows this object's position and rotation, so the rows line up with the house.
        private Vector2Int CellOf(Vector3 position)
        {
            return FarmGrid.CellOf(transform.InverseTransformPoint(position), config.CellSize);
        }

        private Vector3 CenterOf(Vector2Int cell, float height)
        {
            var center = transform.TransformPoint(FarmGrid.CenterOf(cell, config.CellSize, 0f));
            center.y = height;
            return center;
        }

        private void Awake()
        {
            if (config != null)
            {
                Can = new WateringCan(config.CanCapacity);
            }

            _random ??= new System.Random();
        }

        // Only when the whole harvest fits in the bag; otherwise the crop stays ripe.
        private bool TryHarvest(Vector2Int cell)
        {
            if (!Plots.TryGet(cell, out var plot) || !plot.HasCrop || crops == null)
            {
                return false;
            }

            var crop = crops.Find(plot.CropId);
            if (crop == null || crop.Harvest == null || !Plots.IsRipe(cell, crop.GrowthDays))
            {
                return false;
            }

            var quantity = crop.RollHarvest(_random);
            if (bag.Inventory.SpaceFor(crop.Harvest) < quantity)
            {
                Debug.Log($"[Farm] No room in the bag for {quantity} {crop.Harvest.Id}");
                return false;
            }

            bag.Inventory.Add(crop.Harvest, quantity);
            Plots.Harvest(cell, crop.GrowthDays, crop.RegrowDays);
            Plots.TryGet(cell, out var after);
            ShowPlant(cell, after.HasCrop ? crop : null, after.DaysGrown);
            eventBus.Publish(new CropHarvested(crop.Id, crop.Harvest.Id, quantity));
            Debug.Log($"[Farm] Harvested {quantity} {crop.Harvest.Id} on {cell}");
            return true;
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<GroundClicked>(OnGroundClicked);
            eventBus.Subscribe<TargetClicked>(OnTargetClicked);
            eventBus.Subscribe<HotbarSlotSelected>(OnSlotSelected);
            eventBus.Subscribe<GameLoaded>(OnLoaded);
            eventBus.Subscribe<NightPassed>(OnNightPassed);
            eventBus.Subscribe<GameSaving>(OnSaving);
            _subscribed = true;
        }

        // So the hotbar shows the water level from the start.
        private void Start()
        {
            AnnounceCan();
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
            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
            eventBus.Unsubscribe<NightPassed>(OnNightPassed);
            eventBus.Unsubscribe<GameSaving>(OnSaving);
            _subscribed = false;
        }

        private void OnGroundClicked(GroundClicked message)
        {
            UseAt(message.Point);
        }

        private void OnSlotSelected(HotbarSlotSelected message)
        {
            _selectedSlot = message.Index;
        }

        private void OnLoaded(GameLoaded message)
        {
            foreach (var model in _soil.Values)
            {
                Destroy(model);
            }

            foreach (var model in _plants.Values)
            {
                Destroy(model);
            }

            ClearWet();
            _soil.Clear();
            _plants.Clear();
            foreach (var bubble in _bubbles.Values)
            {
                Destroy(bubble);
            }

            _bubbles.Clear();
            Can = new WateringCan(config.CanCapacity, message.Data.wateringCanUsed);
            AnnounceCan();

            _cutGrass.Clear();
            var cut = new HashSet<string>(message.Data.cutGrass ?? new List<string>());
            foreach (var clump in grass)
            {
                if (clump != null && cut.Contains(clump.Id))
                {
                    Cut(clump);
                }
            }

            var dropped = Plots.Restore(message.Data.plots, id => crops != null && crops.Find(id) != null);
            if (dropped > 0)
            {
                Debug.LogWarning($"[Farm] {dropped} saved crop(s) no longer exist; their cells stay tilled.");
            }

            foreach (var pair in Plots.All)
            {
                ShowSoil(pair.Key);
                if (pair.Value.HasCrop)
                {
                    ShowPlant(pair.Key, crops.Find(pair.Value.CropId), pair.Value.DaysGrown);
                }
            }
        }

        private void OnSaving(GameSaving message)
        {
            message.Data.plots = Plots.ToSave();
            message.Data.cutGrass = new List<string>(_cutGrass);
            message.Data.wateringCanUsed = Can.Used;
        }

        // Watered crops grow one day and the soil dries. The morning reload shows it anyway,
        // but the models follow right away so nothing depends on that reload.
        private void OnNightPassed(NightPassed message)
        {
            var grown = Plots.PassNight(id => crops != null && crops.Find(id) != null ? crops.Find(id).GrowthDays : 0);
            ClearWet();
            foreach (var pair in Plots.All)
            {
                if (pair.Value.HasCrop)
                {
                    ShowPlant(pair.Key, crops.Find(pair.Value.CropId), pair.Value.DaysGrown);
                }
            }

            Debug.Log($"[Farm] Night of day {message.Day}: {grown} crop(s) grew");
        }

        private bool TryWater(Vector2Int cell)
        {
            if (!Plots.IsTilled(cell) || Can.IsEmpty || !Plots.Water(cell))
            {
                return false;
            }

            Can.Pour();
            ShowWet(cell, true);
            AnnounceCan();
            eventBus.Publish(new ToolUsed(config.CanStamina));
            return true;
        }

        // Darkens the soil model itself rather than laying something over it, so the two always line up.
        private void ShowWet(Vector2Int cell, bool wet)
        {
            if (wet)
            {
                _wet.Add(cell);
            }
            else
            {
                _wet.Remove(cell);
            }

            if (!_soil.TryGetValue(cell, out var model))
            {
                return;
            }

            _tint ??= new MaterialPropertyBlock();
            _tint.Clear();
            _tint.SetColor(BaseColorId, config.WetSoilTint);
            foreach (var soilRenderer in model.GetComponentsInChildren<Renderer>())
            {
                soilRenderer.SetPropertyBlock(wet ? _tint : null);
            }
        }

        private bool NearWell(Vector3 point)
        {
            if (well == null)
            {
                return false;
            }

            var wellPosition = well.position;
            return Flat(point - wellPosition).sqrMagnitude <= config.WellClickRadius * config.WellClickRadius
                && Flat(player.position - wellPosition).sqrMagnitude <= config.WellReach * config.WellReach;
        }

        private bool RefillCan()
        {
            if (!Can.Refill())
            {
                return false;
            }

            Debug.Log("[Farm] Watering can filled");
            AnnounceCan();
            return true;
        }

        private void AnnounceCan()
        {
            if (eventBus != null && Can != null && config.WateringCan != null)
            {
                eventBus.Publish(new WateringCanChanged(config.WateringCan.Id, Can.Water, Can.Capacity));
            }
        }

        private void ClearWet()
        {
            foreach (var cell in new List<Vector2Int>(_wet))
            {
                ShowWet(cell, false);
            }
        }

        private static Vector3 Flat(Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }

        // Cuts every grass clump in a half circle (by default) in front of the player. Returns how many were cut.
        private int Swing(Vector3 facing)
        {
            var origin = player.position;
            var count = 0;
            foreach (var clump in grass)
            {
                if (clump != null && !clump.IsCut && FarmGrid.InSwing(origin, facing, clump.transform.position, config.SickleRadius, config.SickleArc))
                {
                    Cut(clump);
                    count++;
                }
            }

            return count;
        }

        private void Cut(CuttableGrass clump)
        {
            clump.Cut();
            if (!string.IsNullOrEmpty(clump.Id))
            {
                _cutGrass.Add(clump.Id);
            }
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

        private bool TryTill(Vector2Int cell)
        {
            if (Plots.IsTilled(cell) || Blocked(cell) || !Plots.Till(cell))
            {
                return false;
            }

            ShowSoil(cell);
            Debug.Log($"[Farm] Tilled {cell}");
            eventBus.Publish(new ToolUsed(config.HoeStamina));
            return true;
        }

        private bool TryPlant(Vector2Int cell, CropData crop, ItemData seed)
        {
            if (!Plots.CanPlant(cell) || !bag.Inventory.Remove(seed, 1))
            {
                return false;
            }

            Plots.Plant(cell, crop.Id);
            ShowPlant(cell, crop, 0);
            Debug.Log($"[Farm] Planted {crop.Id} on {cell}");
            return true;
        }

        // Anything solid on the cell (house, tree, rock, fence, path...) other than the player, or uncut grass.
        private bool Blocked(Vector2Int cell)
        {
            var size = config.CellSize;
            var center = CenterOf(cell, transform.position.y + config.ObstacleCheckClearance + config.ObstacleCheckHeight / 2f);
            var halfExtents = new Vector3(size * 0.45f, config.ObstacleCheckHeight / 2f, size * 0.45f);
            var count = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlaps, transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var found = _overlaps[i];
                if (found.isTrigger)
                {
                    // Other triggers (interaction spots...) do not stop the hoe; grass does.
                    if (found.GetComponent<CuttableGrass>() != null)
                    {
                        return true;
                    }
                }
                else if (player == null || !found.transform.IsChildOf(player))
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowSoil(Vector2Int cell)
        {
            if (config.TilledSoilPrefab != null && !_soil.ContainsKey(cell))
            {
                _soil[cell] = Spawn(config.TilledSoilPrefab, cell);
            }
        }

        private void ShowPlant(Vector2Int cell, CropData crop, int daysGrown)
        {
            var prefab = crop != null ? crop.StagePrefab(crop.StageForDay(daysGrown)) : null;
            if (_plants.TryGetValue(cell, out var previous))
            {
                Destroy(previous);
                _plants.Remove(cell);
            }

            if (prefab != null)
            {
                _plants[cell] = Spawn(prefab, cell);
            }

            ShowRipeBubble(cell, crop != null && daysGrown >= crop.GrowthDays ? crop.Harvest : null);
        }

        // A bubble with the harvest's icon above a ripe crop; it can be clicked to pick it.
        private void ShowRipeBubble(Vector2Int cell, ItemData harvest)
        {
            if (_bubbles.TryGetValue(cell, out var previous))
            {
                Destroy(previous);
                _bubbles.Remove(cell);
            }

            if (harvest == null || config.RipeBubblePrefab == null)
            {
                return;
            }

            var prefab = config.RipeBubblePrefab;
            _plants.TryGetValue(cell, out var plant);
            var groundY = transform.position.y;
            var position = CenterOf(cell, ModelBounds.Top(plant, groundY) + config.RipeBubbleGap);
            var bubble = Instantiate(prefab, position, prefab.transform.rotation, transform);
            bubble.SetActive(true);
            var icon = bubble.transform.Find(BubbleIconName);
            if (icon != null && icon.TryGetComponent<SpriteRenderer>(out var iconRenderer))
            {
                iconRenderer.sprite = harvest.Icon;
            }

            _bubbles[cell] = bubble;
        }

        // A click on a ripe crop's bubble picks that crop, if the player is close enough.
        private void OnTargetClicked(TargetClicked message)
        {
            if (message.Target == null || player == null)
            {
                return;
            }

            foreach (var pair in _bubbles)
            {
                if (pair.Value != null && message.Target.transform.IsChildOf(pair.Value.transform))
                {
                    if (FarmGrid.InReach(CellOf(player.position), pair.Key, config.Reach))
                    {
                        TryHarvest(pair.Key);
                    }

                    return;
                }
            }
        }

        // Soil and plants never block the player or the obstacle check.
        private GameObject Spawn(GameObject prefab, Vector2Int cell)
        {
            var model = Instantiate(prefab, CenterOf(cell, transform.position.y), transform.rotation, transform);
            foreach (var modelCollider in model.GetComponentsInChildren<Collider>())
            {
                modelCollider.enabled = false;
            }

            return model;
        }
    }
}
