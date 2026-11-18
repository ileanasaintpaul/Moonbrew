using System;
using System.Collections.Generic;

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

        // Walking back to the door.
        Leaving,
    }

    // Customers who came together and share a table, from their arrival to their departure.
    public class TableGroup
    {
        private readonly List<CustomerData> _members;
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

        public int Table { get; }

        public GroupState State { get; private set; } = GroupState.Arriving;

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

        // Time passes at the table. Returns true when the state changed (ready to order, or out of patience).
        public bool Advance(int minutes)
        {
            if (minutes <= 0 || (State != GroupState.Choosing && State != GroupState.Ready))
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
