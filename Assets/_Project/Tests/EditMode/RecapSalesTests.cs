using Moonbrew.Core;
using Moonbrew.Ui;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class RecapSalesTests
    {
        [Test]
        public void TotalOf_OwnerAndEmployee_AreCountedApart()
        {
            var recap = new DayRecap();
            recap.AddSale("dish_cake", 25, false);
            recap.AddSale("dish_cake", 25, false);
            recap.AddSale("dish_juice", 10, true);

            var owner = RecapSales.TotalOf(recap, false);
            var employee = RecapSales.TotalOf(recap, true);

            Assert.AreEqual((2, 50), (owner.Customers, owner.Money));
            Assert.AreEqual((1, 10), (employee.Customers, employee.Money));
            Assert.AreEqual(60, RecapSales.Earned(recap));
        }

        [Test]
        public void Dishes_SameDishByBoth_MakeOneLine()
        {
            var recap = new DayRecap();
            recap.AddSale("dish_cake", 25, false);
            recap.AddSale("dish_cake", 20, true);
            recap.AddSale("dish_juice", 10, true);

            var dishes = RecapSales.Dishes(recap);

            Assert.AreEqual(2, dishes.Count);
            Assert.AreEqual(("dish_cake", 2, 45), (dishes[0].dishId, dishes[0].count, dishes[0].money));
        }

        [Test]
        public void TotalOf_NoSales_IsZero()
        {
            var total = RecapSales.TotalOf(new DayRecap(), false);

            Assert.AreEqual((0, 0), (total.Customers, total.Money));
        }

        [Test]
        public void Dishes_NullRecapOrSales_AreEmpty()
        {
            Assert.IsEmpty(RecapSales.Dishes(null));
            Assert.IsEmpty(RecapSales.Dishes(new DayRecap { sales = null }));
            Assert.AreEqual(0, RecapSales.Earned(null));
        }

        [Test]
        public void AddSale_InvalidSale_IsIgnored()
        {
            var recap = new DayRecap();

            recap.AddSale("", 10, false);
            recap.AddSale("dish_cake", -1, false);
            recap.AddSale("dish_cake", 10, false, 0);

            Assert.IsEmpty(recap.sales);
        }

        [Test]
        public void AddSale_WithCount_AddsThatMany()
        {
            var recap = new DayRecap();

            recap.AddSale("dish_cake", 40, false, 2);
            recap.AddSale("dish_cake", 20, false);

            Assert.AreEqual((3, 60), (recap.sales[0].count, recap.sales[0].money));
        }
    }
}
