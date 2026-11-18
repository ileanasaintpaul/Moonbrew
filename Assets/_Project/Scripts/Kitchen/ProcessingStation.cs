using System;
using Moonbrew.Core;
using Moonbrew.Inventory;

namespace Moonbrew.Kitchen
{
    // What a station holds: idle, working on a job for some game minutes, or ready to be emptied.
    public class ProcessingStation
    {
        private Job? _job;

        public ProcessingStation(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A station takes at least one batch.");
            }

            Capacity = capacity;
        }

        public int Capacity { get; }

        public string JobId => _job?.Id;

        public ItemData Output => _job?.Output;

        // Batches in the station: each one used the job's input and gives its output.
        public int Batches { get; private set; }

        public int MinutesLeft { get; private set; }

        public bool IsIdle => _job == null;

        public bool IsReady => _job != null && MinutesLeft <= 0;

        public bool IsWorking => _job != null && MinutesLeft > 0;

        public int OutputCount => _job == null ? 0 : Batches * _job.Value.OutputPerBatch;

        // How many batches can start with this many input items; 0 when the station is busy.
        public int BatchesFor(ProcessData process, int availableItems)
        {
            if (!IsIdle || process == null || availableItems <= 0)
            {
                return 0;
            }

            return Math.Min(Capacity, availableItems / Math.Max(1, process.InputQuantity));
        }

        // Starts as many batches as the items allow, up to the capacity. Returns how many started.
        public int Load(ProcessData process, int availableItems)
        {
            var batches = BatchesFor(process, availableItems);
            return batches > 0 && Start(process.ToJob(), batches) ? batches : 0;
        }

        // Starts a job directly, e.g. a recipe whose ingredients were already taken. False when busy.
        public bool Start(Job job, int batches = 1)
        {
            if (!IsIdle || job.Output == null || batches < 1)
            {
                return false;
            }

            _job = job;
            Batches = Math.Min(batches, Capacity);
            MinutesLeft = Math.Max(0, job.Minutes);
            return true;
        }

        // Game time passing. Returns true when the batch has just finished.
        public bool Advance(int minutes)
        {
            if (!IsWorking || minutes <= 0)
            {
                return false;
            }

            MinutesLeft = Math.Max(0, MinutesLeft - minutes);
            return MinutesLeft == 0;
        }

        // Empties a finished station. Returns false when nothing is ready.
        public bool Collect()
        {
            if (!IsReady)
            {
                return false;
            }

            _job = null;
            Batches = 0;
            MinutesLeft = 0;
            return true;
        }

        public StationSave ToSave(string stationId)
        {
            return new StationSave { id = stationId, processId = JobId ?? string.Empty, batches = Batches, minutesLeft = MinutesLeft };
        }

        // An unknown job or an empty save leaves the station idle.
        public void Restore(StationSave save, Func<string, Job?> findJob)
        {
            _job = null;
            Batches = 0;
            MinutesLeft = 0;
            var job = save != null && findJob != null ? findJob(save.processId) : null;
            if (job == null || job.Value.Output == null || save.batches < 1)
            {
                return;
            }

            _job = job;
            Batches = Math.Min(save.batches, Capacity);
            MinutesLeft = Math.Max(0, save.minutesLeft);
        }
    }
}
