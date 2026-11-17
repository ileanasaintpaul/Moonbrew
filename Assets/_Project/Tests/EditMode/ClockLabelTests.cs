using System;
using Moonbrew.Core;
using Moonbrew.Ui;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class ClockLabelTests
    {
        private const string Format = "Jour {0} — {1} — {2}";
        private static readonly string[] Names = { "Matin", "Journée", "Soir", "Nuit" };

        [Test]
        public void Format_DayPhaseAndTime_ReturnsReadableLabel()
        {
            Assert.AreEqual("Jour 3 — Soir — 19:40", ClockLabel.Format(Format, Names, 3, DayPhase.Evening, 1180));
        }

        [Test]
        public void Format_LastPhaseAfterMidnight_WrapsTheHour()
        {
            Assert.AreEqual("Jour 1 — Nuit — 01:30", ClockLabel.Format(Format, Names, 1, DayPhase.Night, 1530));
        }

        [Test]
        public void Format_MissingPhaseName_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ClockLabel.Format(Format, new[] { "Matin" }, 1, DayPhase.Night, 1400));
        }

        [Test]
        public void Format_NullFormat_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ClockLabel.Format(null, Names, 1, DayPhase.Morning, 360));
        }

        [Test]
        public void Format_NullNames_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ClockLabel.Format(Format, null, 1, DayPhase.Morning, 360));
        }

        [TestCase(0, "00:00")]
        [TestCase(365, "06:05")]
        [TestCase(1440, "00:00")]
        [TestCase(1560, "02:00")]
        public void Time_Minutes_FormatsAsHoursAndMinutes(int minutes, string expected)
        {
            Assert.AreEqual(expected, ClockLabel.Time(minutes));
        }

        [Test]
        public void Time_NegativeMinutes_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ClockLabel.Time(-1));
        }
    }
}
