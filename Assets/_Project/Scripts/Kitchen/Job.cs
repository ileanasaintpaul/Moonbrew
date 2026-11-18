using Moonbrew.Inventory;

namespace Moonbrew.Kitchen
{
    // What a station is making: a mill process or a cooked recipe. The ID is what the save keeps.
    public readonly struct Job
    {
        public Job(string id, ItemData output, int outputPerBatch, int minutes)
        {
            Id = id;
            Output = output;
            OutputPerBatch = outputPerBatch;
            Minutes = minutes;
        }

        public string Id { get; }

        public ItemData Output { get; }

        public int OutputPerBatch { get; }

        public int Minutes { get; }
    }
}
