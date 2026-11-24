using System.Diagnostics;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [TestFixture]
    public class TimedWaitsTest
    {
        static readonly long Millisecond = Stopwatch.Frequency / 1000;

        static void Wait (TimedWaits waits, bool late)
        {
            var overshoot = late ? TimedWaits.MarginTicks + Millisecond : 0;
            waits.Record (0, 1, Millisecond + overshoot);
        }

        [Test]
        public void PunctualWaitsBlock ()
        {
            var waits = new TimedWaits ();
            for (int i = 0; i < 100; i++)
                Wait (waits, false);
            Assert.IsFalse (waits.Spin);
        }

        [Test]
        public void MostlyLateWaitsSpin ()
        {
            var waits = new TimedWaits ();
            for (int i = 0; i < 31; i++)
                Wait (waits, true);
            Assert.IsFalse (waits.Spin);
            Wait (waits, true);
            Assert.IsTrue (waits.Spin);
        }

        [Test]
        public void HalfLateWaitsBlock ()
        {
            var waits = new TimedWaits ();
            for (int i = 0; i < 100; i++)
                Wait (waits, i % 2 == 0);
            Assert.IsFalse (waits.Spin);
        }

        [Test]
        public void OnlyRecentWaitsCount ()
        {
            var waits = new TimedWaits ();
            for (int i = 0; i < 32; i++)
                Wait (waits, false);
            for (int i = 0; i < 16; i++)
                Wait (waits, true);
            Assert.IsFalse (waits.Spin);
            Wait (waits, true);
            Assert.IsTrue (waits.Spin);
        }
    }
}
