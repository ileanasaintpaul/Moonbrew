using System.Collections.Generic;

namespace Moonbrew.Economy
{
    public static class SaleSplit
    {
        // Shares a table's payment between its dishes in proportion to their prices; what rounding leaves goes to the first.
        // Dishes without a price share it equally.
        public static int[] Shares(IReadOnlyList<int> prices, int payment)
        {
            if (prices == null || prices.Count == 0)
            {
                return new int[0];
            }

            var shares = new int[prices.Count];
            if (payment <= 0)
            {
                return shares;
            }

            long total = 0;
            foreach (var price in prices)
            {
                total += price > 0 ? price : 0;
            }

            var given = 0;
            for (var i = 0; i < shares.Length; i++)
            {
                shares[i] = total > 0 ? (int)(payment * (long)(prices[i] > 0 ? prices[i] : 0) / total) : payment / shares.Length;
                given += shares[i];
            }

            shares[0] += payment - given;
            return shares;
        }
    }
}
