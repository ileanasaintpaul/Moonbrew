using Moonbrew.Economy;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class SaleSplitTests
    {
        [Test]
        public void Shares_DifferentPrices_FollowThePrices()
        {
            CollectionAssert.AreEqual(new[] { 10, 20 }, SaleSplit.Shares(new[] { 10, 20 }, 30));
        }

        [Test]
        public void Shares_WithBonus_KeepTheWholePayment()
        {
            var shares = SaleSplit.Shares(new[] { 10, 10, 20 }, 50);

            Assert.AreEqual(50, shares[0] + shares[1] + shares[2]);
            Assert.AreEqual(25, shares[2]);
        }

        [Test]
        public void Shares_NoPrices_SplitEqually()
        {
            var shares = SaleSplit.Shares(new[] { 0, 0, 0 }, 10);

            CollectionAssert.AreEqual(new[] { 4, 3, 3 }, shares);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Shares_NoPayment_AreZero(int payment)
        {
            CollectionAssert.AreEqual(new[] { 0, 0 }, SaleSplit.Shares(new[] { 10, 20 }, payment));
        }

        [Test]
        public void Shares_NullOrEmpty_AreEmpty()
        {
            Assert.IsEmpty(SaleSplit.Shares(null, 10));
            Assert.IsEmpty(SaleSplit.Shares(new int[0], 10));
        }
    }
}
