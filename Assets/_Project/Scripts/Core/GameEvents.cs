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

    public readonly struct Interacted
    {
        public Interacted(string interactionId)
        {
            InteractionId = interactionId;
        }

        public string InteractionId { get; }
    }
}
