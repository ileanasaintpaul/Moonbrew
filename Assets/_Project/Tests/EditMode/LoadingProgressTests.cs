using Moonbrew.Ui;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class LoadingProgressTests
    {
        [Test]
        public void Tick_SceneNotReady_StaysOpaque()
        {
            var progress = new LoadingProgress(1f, 0.5f);

            progress.Tick(10f);

            Assert.AreEqual(1f, progress.Alpha);
            Assert.IsFalse(progress.IsDone);
        }

        [Test]
        public void Tick_ReadyBeforeTheMinimum_StaysOpaque()
        {
            var progress = new LoadingProgress(1f, 0.5f);
            progress.SceneReady();

            progress.Tick(0.5f);

            Assert.AreEqual(1f, progress.Alpha);
        }

        [Test]
        public void Tick_ReadyAfterTheMinimum_Fades()
        {
            var progress = new LoadingProgress(1f, 0.5f);
            progress.Tick(1f);
            progress.SceneReady();

            progress.Tick(0.25f);

            Assert.AreEqual(0.5f, progress.Alpha, 0.001f);
            Assert.IsFalse(progress.IsDone);
        }

        [Test]
        public void Tick_FadeOver_IsDone()
        {
            var progress = new LoadingProgress(0f, 0.5f);
            progress.SceneReady();

            progress.Tick(0.6f);

            Assert.AreEqual(0f, progress.Alpha);
            Assert.IsTrue(progress.IsDone);
        }

        [Test]
        public void Tick_NoFadeTime_EndsAtOnce()
        {
            var progress = new LoadingProgress(0f, 0f);
            progress.SceneReady();

            progress.Tick(0.01f);

            Assert.IsTrue(progress.IsDone);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Tick_NoTimePassed_ChangesNothing(float seconds)
        {
            var progress = new LoadingProgress(0f, 0.5f);
            progress.SceneReady();

            progress.Tick(seconds);

            Assert.AreEqual(1f, progress.Alpha);
        }

        [TestCase(0f, 0)]
        [TestCase(2.5f, 3)]
        [TestCase(100f, 7)]
        public void Twinkle_AnyTime_StaysBetweenZeroAndOne(float time, int index)
        {
            var value = LoadingProgress.Twinkle(time, index, 3f);

            Assert.GreaterOrEqual(value, 0f);
            Assert.LessOrEqual(value, 1f);
        }
    }
}
