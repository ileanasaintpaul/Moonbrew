using System.Linq;
using Moonbrew.Player;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class ProgressionTests
    {
        private static Progression Create() => new Progression(new[] { 50, 80 }, 1);

        [Test]
        public void AddXp_BelowTheNextLevel_StaysAtLevelOne()
        {
            var progression = Create();

            Assert.IsEmpty(progression.AddXp(30));

            Assert.AreEqual((1, 30), (progression.Level, progression.Xp));
        }

        [Test]
        public void AddXp_EnoughForALevel_ReachesItAndKeepsTheRest()
        {
            var progression = Create();

            CollectionAssert.AreEqual(new[] { 2 }, progression.AddXp(60));

            Assert.AreEqual((2, 10, 1), (progression.Level, progression.Xp, progression.SkillPoints));
        }

        [Test]
        public void AddXp_SeveralLevelsAtOnce_ReachesEachOne()
        {
            var progression = Create();

            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, progression.AddXp(50 + 80 + 80));

            Assert.AreEqual(3, progression.SkillPoints);
        }

        [Test]
        public void XpToNext_PastTheCurve_RepeatsItsLastStep()
        {
            var progression = Create();
            progression.AddXp(50 + 80 + 80 + 80);

            Assert.AreEqual(80, progression.XpToNext);
        }

        [TestCase(0)]
        [TestCase(-20)]
        public void AddXp_ZeroOrNegative_ChangesNothing(int amount)
        {
            var progression = Create();

            Assert.IsEmpty(progression.AddXp(amount));

            Assert.AreEqual((1, 0), (progression.Level, progression.Xp));
        }

        [Test]
        public void FirstCraft_SameItemTwice_IsFirstOnlyOnce()
        {
            var progression = Create();

            Assert.IsTrue(progression.FirstCraft("dish_cake"));
            Assert.IsFalse(progression.FirstCraft("dish_cake"));
        }

        [Test]
        public void FirstCraft_EmptyOrNull_IsNever()
        {
            var progression = Create();

            Assert.IsFalse(progression.FirstCraft(""));
            Assert.IsFalse(progression.FirstCraft(null));
        }

        [Test]
        public void Restore_SavedProgress_IsKept()
        {
            var progression = Create();

            progression.Restore(3, 12, 2, new[] { "dish_cake" });

            Assert.AreEqual((3, 12, 2), (progression.Level, progression.Xp, progression.SkillPoints));
            Assert.IsFalse(progression.FirstCraft("dish_cake"));
        }

        [Test]
        public void Restore_InvalidValues_StaysValid()
        {
            var progression = Create();

            progression.Restore(0, -5, -1, null);

            Assert.AreEqual((1, 0, 0), (progression.Level, progression.Xp, progression.SkillPoints));
            Assert.IsEmpty(progression.Crafted.ToList());
        }

        [Test]
        public void Constructor_NoCurve_StillLevelsUp()
        {
            var progression = new Progression(null, 1);

            Assert.IsNotEmpty(progression.AddXp(1000));
        }
    }
}
