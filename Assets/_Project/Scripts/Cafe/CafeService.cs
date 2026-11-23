using System;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;

namespace Moonbrew.Cafe
{
    // Who comes into the café, when, and at which table. Game time is given in minutes since midnight.
    public class CafeService
    {
        private readonly CafeConfig _config;
        private readonly IReadOnlyList<CustomerData> _customers;
        private readonly int[] _tableSeats;
        private readonly TableGroup[] _tables;
        private readonly Random _random;
        private int _nextArrival = -1;

        // tableSeats: the number of seats of each table.
        public CafeService(CafeConfig config, IReadOnlyList<int> tableSeats, IReadOnlyList<CustomerData> customers, Random random)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _customers = customers ?? new CustomerData[0];
            _tableSeats = new int[tableSeats?.Count ?? 0];
            for (var i = 0; i < _tableSeats.Length; i++)
            {
                _tableSeats[i] = Math.Max(0, tableSeats[i]);
            }

            _tables = new TableGroup[_tableSeats.Length];
        }

        // The group at each table, or null when the table is free.
        public IReadOnlyList<TableGroup> Tables => _tables;

        public bool IsOpen(int minute) => minute >= _config.OpenMinute && minute < _config.CloseMinute;

        // A group comes in when its time has come, the café is open and a table fits it; null otherwise.
        // The first call of the day only sets the first arrival.
        // Villagers are the important customers: they only come when the owner is there.
        public TableGroup TryArrive(int minute, bool ownerPresent = true)
        {
            if (_nextArrival < 0)
            {
                _nextArrival = Math.Max(minute, _config.OpenMinute) + NextGap();
            }

            if (!IsOpen(minute) || minute < _nextArrival)
            {
                return null;
            }

            _nextArrival = minute + NextGap();
            var members = PickMembers(LargestFreeTable(), ownerPresent);
            if (members.Count == 0)
            {
                return null;
            }

            var table = PickTable(members.Count);
            var chooseMinutes = _random.Next(_config.ChooseMinMinutes, _config.ChooseMaxMinutes + 1);
            var group = new TableGroup(members, table, chooseMinutes, _config.PatienceMinutes);
            _tables[table] = group;
            return group;
        }

        // Time passes at every table. Returns the groups whose state changed.
        public List<TableGroup> Advance(int minutes)
        {
            var changed = new List<TableGroup>();
            foreach (var group in _tables)
            {
                if (group != null && group.Advance(minutes))
                {
                    changed.Add(group);
                }
            }

            return changed;
        }

        // Each member picks a dish from the menu. Returns false, changing nothing, when the group
        // is not ready or the menu has nothing it can order.
        public bool TakeOrder(TableGroup group, IReadOnlyList<MenuDish> menu)
        {
            if (group == null || group.State != GroupState.Ready || menu == null)
            {
                return false;
            }

            var dishes = new List<ItemData>();
            foreach (var member in group.Members)
            {
                var dish = PickDish(member, menu);
                if (dish == null)
                {
                    return false;
                }

                dishes.Add(dish);
            }

            return group.TakeOrder(dishes);
        }

        // Takes the group's whole order out of the inventory and returns what the table pays.
        // All or nothing: an incomplete order returns false and removes nothing.
        public bool Serve(TableGroup group, ItemInventory inventory, out int payment)
        {
            payment = 0;
            if (group == null || inventory == null || group.State != GroupState.Ordered)
            {
                return false;
            }

            var dishIds = new List<string>();
            foreach (var dish in group.Order)
            {
                dishIds.Add(dish.Id);
            }

            if (Array.IndexOf(inventory.Covers(dishIds), false) >= 0)
            {
                return false;
            }

            foreach (var dish in group.Order)
            {
                inventory.Remove(dish, 1);
                payment += dish.BasePrice;
            }

            payment += payment * _config.OwnerBonusPercent / 100;
            return group.Serve();
        }

        // The dishes were found elsewhere (the employee's kitchen): the group only pays and goes.
        public bool ServePrepared(TableGroup group, out int payment)
        {
            payment = 0;
            if (group == null || group.State != GroupState.Ordered)
            {
                return false;
            }

            foreach (var dish in group.Order)
            {
                payment += dish.BasePrice;
            }

            return group.Serve();
        }

        // Closing time: every group gets up. Returns the groups that were not leaving yet.
        public List<TableGroup> Close()
        {
            var leaving = new List<TableGroup>();
            foreach (var group in _tables)
            {
                if (group != null && group.Leave())
                {
                    leaving.Add(group);
                }
            }

            return leaving;
        }

        // The group is out of the door: its table is free again.
        public void Remove(TableGroup group)
        {
            if (group != null && group.Table >= 0 && group.Table < _tables.Length && _tables[group.Table] == group)
            {
                _tables[group.Table] = null;
            }
        }

        // A new day: the café is empty and the first arrival is set again.
        public void Reset()
        {
            Array.Clear(_tables, 0, _tables.Length);
            _nextArrival = -1;
        }

        // A loved dish if there is one, else one without an opinion; a disliked dish only when nothing else is left.
        private ItemData PickDish(CustomerData customer, IReadOnlyList<MenuDish> menu)
        {
            var byTaste = new Dictionary<Taste, List<ItemData>>
            {
                { Taste.Loved, new List<ItemData>() },
                { Taste.Neutral, new List<ItemData>() },
                { Taste.Disliked, new List<ItemData>() },
            };

            foreach (var entry in menu)
            {
                var dish = _config.Items != null ? _config.Items.Find(entry.DishId) : null;
                if (dish == null)
                {
                    continue;
                }

                var ingredients = new List<ItemData>();
                foreach (var id in entry.IngredientIds ?? new string[0])
                {
                    ingredients.Add(_config.Items.Find(id));
                }

                byTaste[customer.TasteOf(dish, ingredients)].Add(dish);
            }

            foreach (var taste in new[] { Taste.Loved, Taste.Neutral, Taste.Disliked })
            {
                if (byTaste[taste].Count > 0)
                {
                    return byTaste[taste][_random.Next(byTaste[taste].Count)];
                }
            }

            return null;
        }

        private int NextGap() => _random.Next(_config.ArrivalMinMinutes, _config.ArrivalMaxMinutes + 1);

        private int LargestFreeTable()
        {
            var largest = 0;
            for (var i = 0; i < _tables.Length; i++)
            {
                if (_tables[i] == null)
                {
                    largest = Math.Max(largest, _tableSeats[i]);
                }
            }

            return largest;
        }

        // A villager alone, or passing customers (each one at most once in the group). Nobody already inside comes again.
        private List<CustomerData> PickMembers(int seats, bool ownerPresent)
        {
            var members = new List<CustomerData>();
            if (seats == 0)
            {
                return members;
            }

            var villagers = ownerPresent ? Available(CustomerKind.Villager) : new List<CustomerData>();
            var passing = Available(CustomerKind.Passing);
            if (villagers.Count > 0 && (passing.Count == 0 || _random.NextDouble() < _config.VillagerChance))
            {
                members.Add(villagers[_random.Next(villagers.Count)]);
                return members;
            }

            if (passing.Count == 0)
            {
                return members;
            }

            var size = _random.Next(1, Math.Min(Math.Min(_config.MaxGroupSize, seats), passing.Count) + 1);
            for (var i = 0; i < size && passing.Count > 0; i++)
            {
                var index = _random.Next(passing.Count);
                members.Add(passing[index]);
                passing.RemoveAt(index);
            }

            return members;
        }

        private List<CustomerData> Available(CustomerKind kind)
        {
            var available = new List<CustomerData>();
            foreach (var customer in _customers)
            {
                if (customer != null && customer.Kind == kind && !IsInside(customer))
                {
                    available.Add(customer);
                }
            }

            return available;
        }

        private bool IsInside(CustomerData customer)
        {
            foreach (var group in _tables)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (var member in group.Members)
                {
                    if (member == customer)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // A free table where the whole group sits, picked at random so the room fills evenly.
        private int PickTable(int size)
        {
            var fitting = new List<int>();
            for (var i = 0; i < _tables.Length; i++)
            {
                if (_tables[i] == null && _tableSeats[i] >= size)
                {
                    fitting.Add(i);
                }
            }

            return fitting[_random.Next(fitting.Count)];
        }
    }
}
