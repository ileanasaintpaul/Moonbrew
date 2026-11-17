using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Kitchen
{
    // One transformation done by a station, e.g. wheat into flour.
    [CreateAssetMenu(menuName = "Moonbrew/Kitchen/Process", fileName = "SO_Process_")]
    public class ProcessData : ScriptableObject
    {
        [Tooltip("Stable snake_case ID stored in saves, e.g. process_flour. Never change it once shipped.")]
        [SerializeField] private string id;
        [SerializeField] private ItemData input;
        [SerializeField, Min(1)] private int inputQuantity = 1;
        [SerializeField] private ItemData output;
        [SerializeField, Min(1)] private int outputQuantity = 1;
        [Tooltip("Game minutes for a whole batch, however many items it holds.")]
        [SerializeField, Min(0)] private int minutes = 60;

        public string Id => id;

        public ItemData Input => input;

        public int InputQuantity => inputQuantity;

        public ItemData Output => output;

        public int OutputQuantity => outputQuantity;

        public int Minutes => minutes;

        // For processes built from code.
        public void Init(string processId, ItemData inputItem, int inputCount, ItemData outputItem, int outputCount, int durationMinutes)
        {
            id = processId;
            input = inputItem;
            inputQuantity = inputCount;
            output = outputItem;
            outputQuantity = outputCount;
            minutes = durationMinutes;
        }
    }
}
