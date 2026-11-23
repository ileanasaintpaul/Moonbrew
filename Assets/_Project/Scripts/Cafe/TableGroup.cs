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
    }

    // Customers who came together and share a table, from their arrival to their departure.
    public class TableGroup
    {
        private readonly List<CustomerData> _members;
        private readonly List<ItemData> _order = new List<ItemData>();
        private readonly int _patienceMinutes;
        private int _seated;
        private int _minutesLeft;

        public TableGroup(IEnumerable<CustomerData> members, int table, int chooseMinutes, int patienceMinutes)
        {
            _members = new List<CustomerData>(members ?? throw new ArgumentNullException(nameof(members)));
            if (_members.Count == 0)
            {
                throw new ArgumentException("A group needs at least one customer.", nameof(members));
            }

            Table = table;
            _minutesLeft = Math.Max(0, chooseMinutes);
            _patienceMinutes = Math.Max(1, patienceMinutes);
        }

        public IReadOnlyList<CustomerData> Members => _members;

        // One dish per member, in the same order; empty until the order is taken.
        public IReadOnlyList<ItemData> Order => _order;

        public int Table { get; }

        public GroupState State { get; private set; } = GroupState.Arriving;

        public bool IsServed { get; private set; }

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

        // Time passes at the table. Returns true when the state changed (ready to order, or out of patience).
        public bool Advance(int minutes)
        {
            if (minutes <= 0 || (State != GroupState.Choosing && State != GroupState.Ready && State != GroupState.Ordered))
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

        // The group got its order: it pays and goes. Returns false unless it was waiting for its dishes.
        public bool Serve()
        {
            if (State != GroupState.Ordered)
            {
                return false;
            }

            IsServed = true;
            State = GroupState.Leaving;
            return true;
        }

        // The group gets up and goes, whatever it was doing. Returns false when it was already leaving.
        public bool Leave()
        {
            if (State == GroupState.Leaving)
            {
                return false;
            }

            State = GroupState.Leaving;
            return true;
        }
    }
}
