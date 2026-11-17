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

    public readonly struct Interacted
    {
        public Interacted(string interactionId)
        {
            InteractionId = interactionId;
        }

        public string InteractionId { get; }
    }
}
