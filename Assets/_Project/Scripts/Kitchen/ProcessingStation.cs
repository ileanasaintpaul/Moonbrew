using System;
using Moonbrew.Core;

namespace Moonbrew.Kitchen
{
    // What a station holds: idle, working on a batch for some game minutes, or ready to be emptied.
    public class ProcessingStation
    {
        public ProcessingStation(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A station takes at least one batch.");
            }

            Capacity = capacity;
        }

        public int Capacity { get; }

        public ProcessData Process { get; private set; }

        // Batches in the station: each one used the process input and gives its output.
        public int Batches { get; private set; }

        public int MinutesLeft { get; private set; }

        public bool IsIdle => Process == null;

        public bool IsReady => Process != null && MinutesLeft <= 0;

        public bool IsWorking => Process != null && MinutesLeft > 0;

        public int OutputCount => Process == null ? 0 : Batches * Process.OutputQuantity;

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
            if (batches == 0)
            {
                return 0;
            }

            Process = process;
            Batches = batches;
            MinutesLeft = Math.Max(0, process.Minutes);
            return batches;
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

            Process = null;
            Batches = 0;
            MinutesLeft = 0;
            return true;
        }

        public StationSave ToSave(string stationId)
        {
            return new StationSave { id = stationId, processId = Process != null ? Process.Id : string.Empty, batches = Batches, minutesLeft = MinutesLeft };
        }

        // An unknown process or an empty save leaves the station idle.
        public void Restore(StationSave save, Func<string, ProcessData> findProcess)
        {
            Process = null;
            Batches = 0;
            MinutesLeft = 0;
            var process = save != null && findProcess != null ? findProcess(save.processId) : null;
            if (process == null || save.batches < 1)
            {
                return;
            }

            Process = process;
            Batches = Math.Min(save.batches, Capacity);
            MinutesLeft = Math.Max(0, save.minutesLeft);
        }
    }
}
