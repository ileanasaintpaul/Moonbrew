using System.Collections.Generic;
using Moonbrew.Core;

namespace Moonbrew.Ui
{
    // What the recap shows of the day's sales: totals by who served, and one line per dish.
    public static class RecapSales
    {
        public struct Total
        {
            public int Customers;
            public int Money;
        }

        // One customer per dish: everyone at a table orders one.
        public static Total TotalOf(DayRecap recap, bool byEmployee)
        {
            var total = new Total();
            foreach (var sale in Sales(recap))
            {
                if (sale != null && sale.byEmployee == byEmployee)
                {
                    total.Customers += sale.count;
                    total.Money += sale.money;
                }
            }

            return total;
        }

        public static int Earned(DayRecap recap)
        {
            return TotalOf(recap, false).Money + TotalOf(recap, true).Money;
        }

        // The dishes sold, whoever served them, in the order first sold.
        public static List<SaleEntry> Dishes(DayRecap recap)
        {
            var dishes = new List<SaleEntry>();
            foreach (var sale in Sales(recap))
            {
                if (sale == null || string.IsNullOrEmpty(sale.dishId))
                {
                    continue;
                }

                var line = dishes.Find(dish => dish.dishId == sale.dishId);
                if (line == null)
                {
                    line = new SaleEntry { dishId = sale.dishId };
                    dishes.Add(line);
                }

                line.count += sale.count;
                line.money += sale.money;
            }

            return dishes;
        }

        private static IEnumerable<SaleEntry> Sales(DayRecap recap)
        {
            return recap != null && recap.sales != null ? recap.sales : new List<SaleEntry>();
        }
    }
}
