using System.Collections.Generic;
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
    // Data holds what was chosen when the game was created (the owner's name and look).
    public readonly struct NewGameStarted
    {
        public NewGameStarted(SaveData data)
        {
            Data = data;
        }

        public SaveData Data { get; }
    }

    // A save was loaded: modules restore their state from it.
    public readonly struct GameLoaded
    {
        public GameLoaded(SaveData data, bool sameDay = false)
        {
            Data = data;
            SameDay = sameDay;
        }

        public SaveData Data { get; }

        // The day goes on after a walk to another scene, rather than opening from the save.
        public bool SameDay { get; }
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

    // A café customer went out of the door.
    public readonly struct CustomerLeft
    {
        public CustomerLeft(string customerId)
        {
            CustomerId = customerId;
        }

        public string CustomerId { get; }
    }

    // A dish the café can serve, as IDs: Core knows neither items nor recipes.
    public readonly struct MenuDish
    {
        public MenuDish(string dishId, IReadOnlyList<string> ingredientIds)
        {
            DishId = dishId;
            IngredientIds = ingredientIds;
        }

        public string DishId { get; }

        public IReadOnlyList<string> IngredientIds { get; }
    }

    // The café needs today's menu: Kitchen adds the dishes it can make to Menu, synchronously.
    public readonly struct MenuRequested
    {
        public MenuRequested(List<MenuDish> menu)
        {
            Menu = menu;
        }

        public List<MenuDish> Menu { get; }
    }

    // The owner took a table's order: one dish per customer. Table is zero-based.
    public readonly struct OrderTaken
    {
        public OrderTaken(int table, IReadOnlyList<string> dishIds)
        {
            Table = table;
            DishIds = dishIds;
        }

        public int Table { get; }

        public IReadOnlyList<string> DishIds { get; }
    }

    // The owner served a table its whole order, and the table paid. Table is zero-based.
    public readonly struct OrderServed
    {
        public OrderServed(int table, IReadOnlyList<string> dishIds, int payment)
        {
            Table = table;
            DishIds = dishIds;
            Payment = payment;
        }

        public int Table { get; }

        public IReadOnlyList<string> DishIds { get; }

        public int Payment { get; }
    }

    // The balance was restored (Delta 0), earned from or spent.
    public readonly struct MoneyChanged
    {
        public MoneyChanged(int balance, int delta)
        {
            Balance = balance;
            Delta = delta;
        }

        public int Balance { get; }

        public int Delta { get; }
    }

    // A table left without getting its order (out of patience, closing time, night).
    public readonly struct OrderAbandoned
    {
        public OrderAbandoned(int table)
        {
            Table = table;
        }

        public int Table { get; }
    }

    // The player clicked the café fridge from close by: the UI opens its panel.
    public readonly struct FridgeOpened
    {
    }

    // The player clicked a slot of the fridge panel, to move its items to the other side.
    public readonly struct FridgeMoveRequested
    {
        public FridgeMoveRequested(bool fromFridge, int slot, bool wholeStack)
        {
            FromFridge = fromFridge;
            Slot = slot;
            WholeStack = wholeStack;
        }

        // True from the fridge to the owner's café inventory, false the other way.
        public bool FromFridge { get; }

        public int Slot { get; }

        // The whole stack, or a single item.
        public bool WholeStack { get; }
    }

    // The player clicked the café display case from close by: the UI opens its panel.
    public readonly struct DisplayOpened
    {
    }

    // The player clicked a slot of the display panel: take one portion out, or put that cake in.
    public readonly struct DisplayMoveRequested
    {
        public DisplayMoveRequested(bool fromDisplay, int slot)
        {
            FromDisplay = fromDisplay;
            Slot = slot;
        }

        // True for a slot of the display case, false for a slot of the owner's café inventory.
        public bool FromDisplay { get; }

        public int Slot { get; }
    }

    // The player walked into a passage to another scene; Arrival names where they come out.
    public readonly struct TravelRequested
    {
        public TravelRequested(string scene, string arrival)
        {
            Scene = scene;
            Arrival = arrival;
        }

        public string Scene { get; }

        public string Arrival { get; }
    }

    // The day was set aside and the next scene is about to load: the screen fades out.
    public readonly struct TravelStarted
    {
    }

    // The player came out in the new scene and the clock runs again: the screen fades in.
    public readonly struct TravelArrived
    {
    }

    // The player picked Continue in the main menu: the last game played goes on.
    public readonly struct ContinueChosen
    {
    }

    // The player picked New Game in the main menu.
    public readonly struct NewGameChosen
    {
    }

    // The player picked one of the saved games in the main menu.
    public readonly struct GameChosen
    {
        public GameChosen(string slotId)
        {
            SlotId = slotId;
        }

        public string SlotId { get; }
    }

    // The player picked Quit in the main menu.
    public readonly struct QuitChosen
    {
    }

    // The player picked Title Screen in the pause menu: the day in progress is left unsaved.
    public readonly struct TitleScreenChosen
    {
    }

    // The player confirmed the owner on the character creation screen: the new game can be created.
    public readonly struct CharacterCreated
    {
        public CharacterCreated(string ownerName, string pronounsId, AppearanceSave appearance)
        {
            OwnerName = ownerName;
            PronounsId = pronounsId;
            Appearance = appearance;
        }

        public string OwnerName { get; }

        public string PronounsId { get; }

        public AppearanceSave Appearance { get; }
    }
}
