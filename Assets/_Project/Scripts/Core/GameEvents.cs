using UnityEngine;

namespace Moonbrew.Core
{
    // Events shared by all modules, named in the past tense. Publish them on the EventBusAsset.
    public readonly struct DayStarted
    {
        public DayStarted(int day)
        {
            Day = day;
        }

        public int Day { get; }
    }

    public readonly struct DayEnded
    {
        public DayEnded(int day)
        {
            Day = day;
        }

        public int Day { get; }
    }

    public readonly struct PhaseChanged
    {
        public PhaseChanged(int day, DayPhase phase)
        {
            Day = day;
            Phase = phase;
        }

        public int Day { get; }

        public DayPhase Phase { get; }
    }

    // Minutes since midnight of the current day, past 24 h after midnight.
    public readonly struct TimeChanged
    {
        public TimeChanged(int minutes)
        {
            Minutes = minutes;
        }

        public int Minutes { get; }
    }

    // The capture mode's free camera was switched on or off; the player stops reacting to input while it is on.
    public readonly struct FreeCameraToggled
    {
        public FreeCameraToggled(bool active)
        {
            Active = active;
        }

        public bool Active { get; }
    }

    // An item's total changed in an inventory. IDs only: Core does not know the item types.
    public readonly struct InventoryChanged
    {
        public InventoryChanged(string inventoryId, string itemId, int quantity)
        {
            InventoryId = inventoryId;
            ItemId = itemId;
            Quantity = quantity;
        }

        public string InventoryId { get; }

        public string ItemId { get; }

        // The item's new total in that inventory.
        public int Quantity { get; }
    }

    // Zero-based index of the hotbar slot the player now holds.
    public readonly struct HotbarSlotSelected
    {
        public HotbarSlotSelected(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }

    // A fresh game begins: modules set up their starting state (starting items...).
    public readonly struct NewGameStarted
    {
    }

    // A save was loaded: modules restore their state from it.
    public readonly struct GameLoaded
    {
        public GameLoaded(SaveData data)
        {
            Data = data;
        }

        public SaveData Data { get; }
    }

    // The day ended and is being saved: modules write their state and their part of the recap into Data.
    public readonly struct GameSaving
    {
        public GameSaving(SaveData data)
        {
            Data = data;
        }

        public SaveData Data { get; }
    }

    // The night after this day passes, just before the save: modules apply what happens overnight
    // (crops grow, the soil dries...).
    public readonly struct NightPassed
    {
        public NightPassed(int day)
        {
            Day = day;
        }

        public int Day { get; }
    }

    // A ripe crop was picked and its harvest added to the player's bag.
    public readonly struct CropHarvested
    {
        public CropHarvested(string cropId, string itemId, int quantity)
        {
            CropId = cropId;
            ItemId = itemId;
            Quantity = quantity;
        }

        public string CropId { get; }

        public string ItemId { get; }

        public int Quantity { get; }
    }

    // A station's output (flour from the mill...) was collected into the player's bag.
    public readonly struct ItemCrafted
    {
        public ItemCrafted(string stationId, string itemId, int quantity)
        {
            StationId = stationId;
            ItemId = itemId;
            Quantity = quantity;
        }

        public string StationId { get; }

        public string ItemId { get; }

        public int Quantity { get; }
    }

    // The player clicked an idle cooking station: the game menu opens on its recipes to pick one.
    public readonly struct StoveOpened
    {
        public StoveOpened(string stationId)
        {
            StationId = stationId;
        }

        public string StationId { get; }
    }

    // The player picked a recipe to cook at this station, from the game menu.
    public readonly struct RecipeChosen
    {
        public RecipeChosen(string stationId, string recipeId)
        {
            StationId = stationId;
            RecipeId = recipeId;
        }

        public string StationId { get; }

        public string RecipeId { get; }
    }

    // A recipe joined the player's book (bought in the shop...).
    public readonly struct RecipeLearned
    {
        public RecipeLearned(string recipeId)
        {
            RecipeId = recipeId;
        }

        public string RecipeId { get; }
    }

    // The game menu opened or closed; the game pauses while it is open.
    public readonly struct GameMenuToggled
    {
        public GameMenuToggled(bool open)
        {
            Open = open;
        }

        public bool Open { get; }
    }

    // The watering can was used or refilled. ItemId says which item shows the water level.
    public readonly struct WateringCanChanged
    {
        public WateringCanChanged(string itemId, int water, int capacity)
        {
            ItemId = itemId;
            Water = water;
            Capacity = capacity;
        }

        public string ItemId { get; }

        public int Water { get; }

        public int Capacity { get; }
    }

    // The previous day's summary opens the day; the clock waits until DayRecapClosed.
    public readonly struct DayRecapOpened
    {
        public DayRecapOpened(DayRecap recap)
        {
            Recap = recap;
        }

        public DayRecap Recap { get; }
    }

    public readonly struct DayRecapClosed
    {
    }

    // The player reached the bed: the UI asks whether to sleep and end the day.
    public readonly struct SleepOffered
    {
    }

    public readonly struct SleepConfirmed
    {
    }

    // The player clicked the ground (use the item in hand there), at this world point.
    public readonly struct GroundClicked
    {
        public GroundClicked(Vector3 point)
        {
            Point = point;
        }

        public Vector3 Point { get; }
    }

    // The player clicked an object marked as a click target (a station, a ripe crop's bubble...).
    public readonly struct TargetClicked
    {
        public TargetClicked(ClickTarget target)
        {
            Target = target;
        }

        public ClickTarget Target { get; }
    }

    public readonly struct Interacted
    {
        public Interacted(string interactionId)
        {
            InteractionId = interactionId;
        }

        public string InteractionId { get; }
    }
}
