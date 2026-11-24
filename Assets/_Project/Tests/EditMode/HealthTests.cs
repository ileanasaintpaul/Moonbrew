using Moonbrew.Player;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class HealthTests
    {
        [Test]
        public void Lose_Damage_LowersHealth()
        {
            var health = new Health(120);

            health.Lose(30);

            Assert.AreEqual(90, health.Current);
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void Lose_ZeroOrNegative_ChangesNothing(int damage)
        {
            var health = new Health(120);

            health.Lose(damage);

            Assert.AreEqual(120, health.Current);
        }

        [Test]
        public void Lose_MoreThanLeft_StopsAtZero()
        {
            var health = new Health(50);

            health.Lose(80);

            Assert.AreEqual(0, health.Current);
            Assert.IsTrue(health.IsEmpty);
        }

        [Test]
        public void Restore_SavedValue_IsKept()
        {
            var health = new Health(120);

            health.Restore(70);

            Assert.AreEqual(70, health.Current);
        }

        [Test]
        public void Restore_Negative_IsFull()
        {
            var health = new Health(120);
            health.Lose(40);

            health.Restore(-1);

            Assert.AreEqual(120, health.Current);
        }

        [Test]
        public void Restore_AboveTheMaximum_StopsAtTheMaximum()
        {
            var health = new Health(120);

            health.Restore(999);

            Assert.AreEqual(120, health.Current);
        }

        [Test]
        public void Wake_AfterDamage_IsFull()
        {
            var health = new Health(120);
            health.Lose(120);

            health.Wake();

            Assert.AreEqual(120, health.Current);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Constructor_InvalidMax_KeepsOnePoint(int max)
        {
            Assert.AreEqual(1, new Health(max).Max);
        }
    }
}
