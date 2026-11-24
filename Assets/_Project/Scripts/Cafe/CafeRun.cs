using System;
using System.Collections.Generic;

namespace Moonbrew.Cafe
{
    // The café's day, whichever scene the owner is in.
    public class CafeRun
    {
        private readonly CafeConfig _config;

        public CafeRun(CafeConfig config, IReadOnlyList<int> tableSeats, Random random)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Service = new CafeService(config, tableSeats, config.Customers != null ? config.Customers.Customers : null, random);
        }

        public CafeService Service { get; }

        // The last game time heard, -1 before the first one of the day.
        public int LastMinutes { get; set; } = -1;

        public bool Closed { get; set; }

        // The room is not on screen: customers reach their seats at once and leave at once.
        // The employee cashes the bills at the till. Goes minute by minute so arrivals keep their rhythm.
        // Returns the groups that left; paid, if given, gets the groups whose bill was cashed.
        public List<TableGroup> AdvanceAway(int minutes, List<TableGroup> paid = null)
        {
            var gone = new List<TableGroup>();
            RemoveLeaving(gone);
            if (LastMinutes < 0)
            {
                LastMinutes = minutes;
                Service.TryArrive(minutes, false);
                return gone;
            }

            for (var minute = LastMinutes + 1; minute <= minutes; minute++)
            {
                SeatArriving();
                Service.Advance(1);
                CashBills(paid);
                if (!Closed && minute >= _config.CloseMinute)
                {
                    Closed = true;
                    Service.Close();
                }

                RemoveLeaving(gone);
                Service.TryArrive(minute, false);
            }

            SeatArriving();
            LastMinutes = Math.Max(LastMinutes, minutes);
            return gone;
        }

        public void Reset()
        {
            Service.Reset();
            LastMinutes = -1;
            Closed = false;
        }

        private void CashBills(List<TableGroup> paid)
        {
            foreach (var group in Service.Tables)
            {
                if (group != null && Service.Pay(group, out _))
                {
                    paid?.Add(group);
                }
            }
        }

        private void SeatArriving()
        {
            foreach (var group in Service.Tables)
            {
                group?.SitAll();
            }
        }

        private void RemoveLeaving(List<TableGroup> gone)
        {
            foreach (var group in Service.Tables)
            {
                if (group != null && group.State == GroupState.Leaving)
                {
                    gone.Add(group);
                }
            }

            foreach (var group in gone)
            {
                Service.Remove(group);
            }
        }
    }
}
