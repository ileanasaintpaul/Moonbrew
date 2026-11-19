using Moonbrew.Core;
using Moonbrew.Inventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // The sleep prompt and the morning recap. Display only: it shows what the session announces
    // and forwards the player's answer (SleepConfirmed, DayRecapClosed).
    [RequireComponent(typeof(UIDocument))]
    public class DayPanels : MonoBehaviour
    {
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private ItemCatalog catalog;
        [SerializeField] private InventoryConfig[] inventories = new InventoryConfig[0];
        [SerializeField] private VisualTreeAsset sleepPromptTemplate;
        [SerializeField] private VisualTreeAsset recapTemplate;
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private string recapTitleFormat = "Day {0} complete";
        [SerializeField] private string recapLineFormat = "{0}: {1}";
        [SerializeField] private string recapEmptyText = "Nothing changed in your bag or the stock.";

        private UIDocument _document;
        private VisualElement _sleepPrompt;
        private VisualElement _recap;
        private bool _subscribed;

        public bool SleepPromptOpen => _sleepPrompt != null;

        public bool RecapOpen => _recap != null;

        public string RecapTitle => _recap?.Q<Label>("title").text;

        public string RecapLines => _recap?.Q<Label>("lines").text;

        public void Init(EventBusAsset bus, ItemCatalog itemCatalog, InventoryConfig[] inventoryConfigs, VisualTreeAsset sleepPrompt, VisualTreeAsset recap)
        {
            Unsubscribe();
            eventBus = bus;
            catalog = itemCatalog;
            inventories = inventoryConfigs;
            sleepPromptTemplate = sleepPrompt;
            recapTemplate = recap;
            Subscribe();
        }

        // The player's answers; also called by the buttons.
        public void AnswerSleep(bool sleep)
        {
            Close(ref _sleepPrompt);
            if (sleep)
            {
                eventBus.Publish(new SleepConfirmed());
            }
        }

        public void CloseRecap()
        {
            if (_recap != null)
            {
                Close(ref _recap);
                eventBus.Publish(new DayRecapClosed());
            }
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            Close(ref _sleepPrompt);
            Close(ref _recap);
        }

        private void OnSleepOffered(SleepOffered message)
        {
            if (_sleepPrompt != null || !TryOpen(sleepPromptTemplate, "sleep-prompt", out _sleepPrompt))
            {
                return;
            }

            _sleepPrompt.Q<Button>("yes").clicked += () => AnswerSleep(true);
            _sleepPrompt.Q<Button>("no").clicked += () => AnswerSleep(false);
        }

        private void OnRecapOpened(DayRecapOpened message)
        {
            if (_recap != null || !TryOpen(recapTemplate, "day-recap", out _recap))
            {
                return;
            }

            _recap.Q<Label>("title").text = string.Format(recapTitleFormat, message.Recap.day);
            _recap.Q<Label>("lines").text = RecapText.Lines(message.Recap, InventoryName, ItemName, recapLineFormat, recapEmptyText);
            _recap.Q<Button>("continue").clicked += CloseRecap;
        }

        private string InventoryName(string id)
        {
            foreach (var inventory in inventories)
            {
                if (inventory != null && inventory.Id == id)
                {
                    return inventory.DisplayName;
                }
            }

            return null;
        }

        private string ItemName(string id)
        {
            var item = catalog != null ? catalog.Find(id) : null;
            return item != null ? item.DisplayName : null;
        }

        private bool TryOpen(VisualTreeAsset template, string elementName, out VisualElement panel)
        {
            panel = null;
            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null || template == null)
            {
                return false;
            }

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            panel = template.Instantiate().Q(elementName);
            root.Add(panel);
            return true;
        }

        private static void Close(ref VisualElement panel)
        {
            panel?.RemoveFromHierarchy();
            panel = null;
        }

        private void Subscribe()
        {
            if (_subscribed || eventBus == null || !isActiveAndEnabled)
            {
                return;
            }

            eventBus.Subscribe<SleepOffered>(OnSleepOffered);
            eventBus.Subscribe<DayRecapOpened>(OnRecapOpened);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && eventBus != null)
            {
                eventBus.Unsubscribe<SleepOffered>(OnSleepOffered);
                eventBus.Unsubscribe<DayRecapOpened>(OnRecapOpened);
            }

            _subscribed = false;
        }
    }
}
