using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Farming
{
    // The farm's soil: on a ground click near the player, the hoe tills a free cell and seeds are planted
    // on a tilled one, taken from the player's bag. Keeps the scene in step with FarmPlots and saves it.
    public class FarmField : MonoBehaviour
    {
        private const int OverlapBufferSize = 16;

        [SerializeField] private FarmConfig config;
        [SerializeField] private CropCatalog crops;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private InventoryHolder bag;
        [SerializeField] private Transform player;
        [Tooltip("Every grass clump of the farm, filled by the editor setup.")]
        [SerializeField] private CuttableGrass[] grass = new CuttableGrass[0];

        private readonly Collider[] _overlaps = new Collider[OverlapBufferSize];
        private readonly HashSet<string> _cutGrass = new HashSet<string>();
        private readonly Dictionary<Vector2Int, GameObject> _soil = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<Vector2Int, GameObject> _plants = new Dictionary<Vector2Int, GameObject>();
        private bool _subscribed;
        private int _selectedSlot;

        public FarmPlots Plots { get; } = new FarmPlots();

        public bool HasPlantModel(Vector2Int cell) => _plants.ContainsKey(cell);

        public bool HasSoilModel(Vector2Int cell) => _soil.ContainsKey(cell);

        public int CutGrassCount => _cutGrass.Count;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(FarmConfig farmConfig, CropCatalog cropCatalog, EventBusAsset bus, InventoryHolder playerBag, Transform playerTransform, CuttableGrass[] grassClumps = null)
        {
            config = farmConfig;
            crops = cropCatalog;
            eventBus = bus;
            bag = playerBag;
            player = playerTransform;
            grass = grassClumps ?? new CuttableGrass[0];
        }

        // Uses the item in hand towards a ground point. Returns true when something happened.
        public bool UseAt(Vector3 point)
        {
            var item = ItemInHand();
            if (item == null || player == null)
            {
                return false;
            }

            // The sickle swings towards the click, whatever its distance.
            if (item == config.Sickle)
            {
                return Swing(point - player.position) > 0;
            }

            var cell = FarmGrid.CellOf(point, config.CellSize);
            if (!FarmGrid.InReach(FarmGrid.CellOf(player.position, config.CellSize), cell, config.Reach))
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

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<GroundClicked>(OnGroundClicked);
            eventBus.Subscribe<HotbarSlotSelected>(OnSlotSelected);
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
            eventBus.Unsubscribe<HotbarSlotSelected>(OnSlotSelected);
            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
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

            _soil.Clear();
            _plants.Clear();

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
            message.Data.plots.AddRange(Plots.ToSave());
            message.Data.cutGrass.AddRange(_cutGrass);
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
            var center = FarmGrid.CenterOf(cell, size, transform.position.y + config.ObstacleCheckClearance + config.ObstacleCheckHeight / 2f);
            var halfExtents = new Vector3(size * 0.45f, config.ObstacleCheckHeight / 2f, size * 0.45f);
            var count = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlaps, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Collide);
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
            if (prefab != null)
            {
                _plants[cell] = Spawn(prefab, cell);
            }
        }

        // Soil and plants never block the player or the obstacle check.
        private GameObject Spawn(GameObject prefab, Vector2Int cell)
        {
            var model = Instantiate(prefab, FarmGrid.CenterOf(cell, config.CellSize, transform.position.y), Quaternion.identity, transform);
            foreach (var modelCollider in model.GetComponentsInChildren<Collider>())
            {
                modelCollider.enabled = false;
            }

            return model;
        }
    }
}
