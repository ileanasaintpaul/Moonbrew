using Moonbrew.Player;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class StaminaTests
    {
        [Test]
        public void Spend_Cost_LowersStamina()
        {
            var stamina = new Stamina(100);

            stamina.Spend(4);

            Assert.AreEqual(96, stamina.Current);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Spend_ZeroOrNegative_ChangesNothing(int cost)
        {
            var stamina = new Stamina(100);

            stamina.Spend(cost);

            Assert.AreEqual(100, stamina.Current);
        }

        [Test]
        public void Spend_MoreThanLeft_StopsAtZero()
        {
            var stamina = new Stamina(10);

            stamina.Spend(25);

            Assert.AreEqual(0, stamina.Current);
            Assert.IsTrue(stamina.IsEmpty);
        }

        [Test]
        public void Restore_SavedValue_IsKept()
        {
            var stamina = new Stamina(100);

            stamina.Restore(42);

            Assert.AreEqual(42, stamina.Current);
        }

        [Test]
        public void Restore_Negative_IsFull()
        {
            var stamina = new Stamina(100);
            stamina.Spend(50);

            stamina.Restore(-1);

            Assert.AreEqual(100, stamina.Current);
        }

        [Test]
        public void Restore_AboveTheMaximum_StopsAtTheMaximum()
        {
            var stamina = new Stamina(100);

            stamina.Restore(500);

            Assert.AreEqual(100, stamina.Current);
        }

        [Test]
        public void Wake_AfterSleeping_IsFull()
        {
            var stamina = new Stamina(100);
            stamina.Spend(80);

            stamina.Wake(true, 30);

            Assert.AreEqual(100, stamina.Current);
        }

        [Test]
        public void Wake_WithoutSleeping_IsReduced()
        {
            var stamina = new Stamina(100);

            stamina.Wake(false, 30);

            Assert.AreEqual(70, stamina.Current);
        }

        [Test]
        public void Wake_PenaltyAboveTheMaximum_StopsAtZero()
        {
            var stamina = new Stamina(20);

            stamina.Wake(false, 30);

            Assert.AreEqual(0, stamina.Current);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Constructor_InvalidMax_KeepsOnePoint(int max)
        {
            Assert.AreEqual(1, new Stamina(max).Max);
        }
    }
}
