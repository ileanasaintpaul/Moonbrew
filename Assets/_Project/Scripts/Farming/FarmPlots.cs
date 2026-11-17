using System.Collections.Generic;
using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Farming
{
    // Which cells are tilled and what grows on them. Plain state: the scene objects follow it.
    public class FarmPlots
    {
        private readonly Dictionary<Vector2Int, Plot> _plots = new Dictionary<Vector2Int, Plot>();

        public int Count => _plots.Count;

        public IEnumerable<KeyValuePair<Vector2Int, Plot>> All => _plots;

        public bool IsTilled(Vector2Int cell)
        {
            return _plots.ContainsKey(cell);
        }

        public bool TryGet(Vector2Int cell, out Plot plot)
        {
            return _plots.TryGetValue(cell, out plot);
        }

        // Returns false when the cell is already tilled.
        public bool Till(Vector2Int cell)
        {
            if (_plots.ContainsKey(cell))
            {
                return false;
            }

            _plots[cell] = new Plot(null, 0);
            return true;
        }

        public bool CanPlant(Vector2Int cell)
        {
            return _plots.TryGetValue(cell, out var plot) && !plot.HasCrop;
        }

        // Only on a tilled, empty cell.
        public bool Plant(Vector2Int cell, string cropId)
        {
            if (string.IsNullOrEmpty(cropId) || !CanPlant(cell))
            {
                return false;
            }

            // Seeds sown in already watered soil count as watered today.
            _plots[cell] = new Plot(cropId, 0, _plots[cell].Watered);
            return true;
        }

        // Any tilled cell can be watered, planted or not; watering twice the same day does nothing.
        public bool Water(Vector2Int cell)
        {
            if (!_plots.TryGetValue(cell, out var plot) || plot.Watered)
            {
                return false;
            }

            _plots[cell] = new Plot(plot.CropId, plot.DaysGrown, true);
            return true;
        }

        public bool IsRipe(Vector2Int cell, int growthDays)
        {
            return _plots.TryGetValue(cell, out var plot) && plot.HasCrop && plot.DaysGrown >= growthDays;
        }

        // Picks a ripe crop. A crop that regrows goes back regrowDays before ripe; any other leaves the soil
        // tilled and empty. Returns false when there is nothing ripe on the cell.
        public bool Harvest(Vector2Int cell, int growthDays, int regrowDays)
        {
            if (!IsRipe(cell, growthDays))
            {
                return false;
            }

            var plot = _plots[cell];
            _plots[cell] = regrowDays > 0
                ? new Plot(plot.CropId, System.Math.Max(0, growthDays - regrowDays), plot.Watered)
                : new Plot(null, 0, plot.Watered);
            return true;
        }

        // The night: every watered crop grows one day, up to its growth days, then all the soil dries.
        // Returns how many crops grew.
        public int PassNight(System.Func<string, int> growthDaysOf)
        {
            var grown = 0;
            var cells = new List<Vector2Int>(_plots.Keys);
            foreach (var cell in cells)
            {
                var plot = _plots[cell];
                var days = plot.DaysGrown;
                if (plot.Watered && plot.HasCrop && growthDaysOf != null && days < growthDaysOf(plot.CropId))
                {
                    days++;
                    grown++;
                }

                _plots[cell] = new Plot(plot.CropId, days, false);
            }

            return grown;
        }

        public List<PlotSave> ToSave()
        {
            var saves = new List<PlotSave>();
            foreach (var pair in _plots)
            {
                saves.Add(new PlotSave { x = pair.Key.x, y = pair.Key.y, cropId = pair.Value.CropId ?? string.Empty, daysGrown = pair.Value.DaysGrown });
            }

            return saves;
        }

        // Replaces the state with a save. A crop the game no longer knows leaves its cell tilled and empty;
        // the number of such crops is returned so the caller can report them.
        public int Restore(IEnumerable<PlotSave> saves, System.Func<string, bool> cropExists)
        {
            _plots.Clear();
            var dropped = 0;
            foreach (var save in saves ?? new List<PlotSave>())
            {
                var cropId = string.IsNullOrEmpty(save.cropId) ? null : save.cropId;
                if (cropId != null && cropExists != null && !cropExists(cropId))
                {
                    dropped++;
                    cropId = null;
                }

                _plots[new Vector2Int(save.x, save.y)] = new Plot(cropId, cropId == null ? 0 : Mathf.Max(0, save.daysGrown));
            }

            return dropped;
        }
    }

    public readonly struct Plot
    {
        public Plot(string cropId, int daysGrown, bool watered = false)
        {
            CropId = cropId;
            DaysGrown = daysGrown;
            Watered = watered;
        }

        // Watered today. Never saved: the soil dries every night, before the save.
        public bool Watered { get; }

        // Null on tilled soil with nothing planted.
        public string CropId { get; }

        public int DaysGrown { get; }

        public bool HasCrop => CropId != null;
    }
}
