using System;
using System.Collections.Generic;
using Moonbrew.Inventory;

namespace Moonbrew.Cafe
{
    public enum GroupState
    {
        // Walking from the door to the table.
        Arriving,

        // Seated, choosing what to order.
        Choosing,

        // Ready to order: the "!" bubble shows.
        Ready,

        // The order was taken: waiting for the dishes.
        Ordered,

        // Walking back to the door.
        Leaving,

        // Served: eating at the table.
        Eating,

        // Done eating: waiting at the till to pay.
        Paying,
    }

    // Customers who came together and share a table, from their arrival to their departure.
    public class TableGroup
    {
        private readonly List<CustomerData> _members;
        private readonly List<ItemData> _order = new List<ItemData>();
        private readonly int _patienceMinutes;
        private readonly int _eatMinutes;
        private int _seated;
        private int _minutesLeft;

        public TableGroup(IEnumerable<CustomerData> members, int table, int chooseMinutes, int patienceMinutes, int eatMinutes = 0)
        {
            _members = new List<CustomerData>(members ?? throw new ArgumentNullException(nameof(members)));
            if (_members.Count == 0)
            {
                throw new ArgumentException("A group needs at least one customer.", nameof(members));
            }

            Table = table;
            _minutesLeft = Math.Max(0, chooseMinutes);
            _patienceMinutes = Math.Max(1, patienceMinutes);
            _eatMinutes = Math.Max(0, eatMinutes);
        }

        public IReadOnlyList<CustomerData> Members => _members;

        // One dish per member, in the same order; empty until the order is taken.
        public IReadOnlyList<ItemData> Order => _order;

        public int Table { get; }

        public GroupState State { get; private set; } = GroupState.Arriving;

        public bool IsServed { get; private set; }

        public bool IsPaid { get; private set; }

        // What the group owes once served, paid at the till.
        public int Bill { get; private set; }

        public bool ServedByEmployee { get; private set; }

        // The IDs of the ordered dishes, one per member.
        public List<string> DishIds()
        {
            var ids = new List<string>();
            foreach (var dish in _order)
            {
                ids.Add(dish.Id);
            }

            return ids;
        }

        // One member reached their seat. The group starts choosing once everyone sits.
        // Returns true when this made the whole group seated.
        public bool Sit()
        {
            if (State != GroupState.Arriving)
            {
                return false;
            }

            _seated++;
            if (_seated < _members.Count)
            {
                return false;
            }

            State = GroupState.Choosing;
            return true;
        }

        // The whole group sits at once, when nobody is there to watch it walk in.
        public void SitAll()
        {
            while (State == GroupState.Arriving)
            {
                Sit();
            }
        }

        // Time passes at the table. Returns true when the state changed (ready to order, out of patience, done eating).
        public bool Advance(int minutes)
        {
            if (minutes <= 0 || (State != GroupState.Choosing && State != GroupState.Ready && State != GroupState.Ordered && State != GroupState.Eating))
            {
                return false;
            }

            _minutesLeft -= minutes;
            if (_minutesLeft > 0)
            {
                return false;
            }

            if (State == GroupState.Choosing)
            {
                State = GroupState.Ready;
                _minutesLeft = _patienceMinutes;
            }
            else if (State == GroupState.Eating)
            {
                State = GroupState.Paying;
            }
            else
            {
                State = GroupState.Leaving;
            }

            return true;
        }

        // A ready group orders one dish per member, and patience starts again while it waits for them.
        // Returns false when the group is not ready or the order does not fit it.
        public bool TakeOrder(IReadOnlyList<ItemData> dishes)
        {
            if (State != GroupState.Ready || dishes == null || dishes.Count != _members.Count)
            {
                return false;
            }

            foreach (var dish in dishes)
            {
                if (dish == null)
                {
                    return false;
                }
            }

            _order.AddRange(dishes);
            State = GroupState.Ordered;
            _minutesLeft = _patienceMinutes;
            return true;
        }

        // The group got its order: it eats, then pays this bill at the till. Returns false unless it was waiting for its dishes.
        public bool Serve(int bill = 0, bool byEmployee = false)
        {
            if (State != GroupState.Ordered)
            {
                return false;
            }

            IsServed = true;
            Bill = Math.Max(0, bill);
            ServedByEmployee = byEmployee;
            State = _eatMinutes > 0 ? GroupState.Eating : GroupState.Paying;
            _minutesLeft = _eatMinutes;
            return true;
        }

        // The bill is cashed at the till: the group goes. Returns false unless it was waiting to pay.
        public bool Pay()
        {
            if (State != GroupState.Paying)
            {
                return false;
            }

            IsPaid = true;
            State = GroupState.Leaving;
            return true;
        }

        // The group gets up and goes, whatever it was doing, except a served group, which stays to pay.
        // Returns false when it was already leaving or still owes its bill.
        public bool Leave()
        {
            if (State == GroupState.Leaving || State == GroupState.Eating || State == GroupState.Paying)
            {
                return false;
            }

            State = GroupState.Leaving;
            return true;
        }
    }
}
