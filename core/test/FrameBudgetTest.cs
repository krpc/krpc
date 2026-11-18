using System.Diagnostics;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test
{
    [TestFixture]
    public class FrameBudgetTest
    {
        FrameBudget budget;

        [SetUp]
        public void SetUp ()
        {
            budget = new FrameBudget ();
            budget.Configure (true, 60, 10000);
        }

        static long Ticks (double milliseconds)
        {
            return StopwatchExtensions.MicrosecondsToTicks ((long)(milliseconds * 1000));
        }

        static double Milliseconds (long ticks)
        {
            return ticks * 1000d / Stopwatch.Frequency;
        }

        void Frame (double period, double spent, bool bound = false, bool held = false)
        {
            budget.FrameEnded (Ticks (period), Ticks (spent), bound, held);
        }

        [Test]
        public void StartsAtTheMaximum ()
        {
            Assert.AreEqual (Ticks (10), budget.Budget);
        }

        void Frames (int count, double period, double spent, bool bound = false)
        {
            for (int i = 0; i < count; i++)
                Frame (period, spent, bound);
        }

        [Test]
        public void ShrinksByTheExcessWhileFramesAreSlow ()
        {
            Frames (50, 25, 10);
            // The target period is 1/60 s plus the 2% tolerance, 17 ms
            Assert.That (Milliseconds (budget.Budget), Is.EqualTo (2).Within (0.05));
        }

        [Test]
        public void IgnoresOccasionalSlowFrames ()
        {
            for (int i = 0; i < 20; i++) {
                Frames (7, 16, 10, bound: true);
                Frame (30, 10, bound: true);
            }
            Assert.AreEqual (Ticks (10), budget.Budget);
        }

        [Test]
        public void IgnoresJitterAboutTheTarget ()
        {
            // A frame limiter's frames, a millisecond or two either side of 1/60 s
            for (int i = 0; i < 100; i++) {
                Frame (15.2, 10, bound: true);
                Frame (18.1, 10, bound: true);
                Frame (16.7, 0);
            }
            Assert.AreEqual (Ticks (10), budget.Budget);
        }

        [Test]
        public void GrowsBackThroughJitter ()
        {
            Frames (50, 40, 5);
            for (int i = 0; i < 50; i++) {
                Frame (15.2, 1, bound: true);
                Frame (18.1, 1, bound: true);
            }
            Assert.AreEqual (Ticks (10), budget.Budget);
        }

        [Test]
        public void IgnoresAHitch ()
        {
            Frames (16, 16.7, 10, bound: true);
            Frame (2400, 0);
            Frames (16, 16.7, 10, bound: true);
            Assert.AreEqual (Ticks (10), budget.Budget);
        }

        [Test]
        public void IgnoresTheFirstFramesAfterStarting ()
        {
            Frames (3, 140, 5);
            Frames (16, 16.7, 10, bound: true);
            Assert.AreEqual (Ticks (10), budget.Budget);
        }

        [Test]
        public void CutsDoNotCompound ()
        {
            // Every frame slow, by 8 ms: one cut, by the excess, then none while the frames
            // show the same server time
            Frames (16, 25, 10);
            Assert.That (Milliseconds (budget.Budget), Is.EqualTo (2).Within (0.05));
            Frames (40, 25, 10);
            Assert.That (Milliseconds (budget.Budget), Is.EqualTo (2).Within (0.05));
        }

        [Test]
        public void ShrinksForSlowFramesInBursts ()
        {
            for (int i = 0; i < 30; i++) {
                Frame (25, 10);
                Frame (16, 1);
                Frame (16, 1);
            }
            Assert.That (Milliseconds (budget.Budget), Is.LessThan (4));
        }

        [Test]
        public void ShrinksToTheFloorWhenTheGameAloneIsSlow ()
        {
            Assert.IsFalse (budget.AtFloor);
            Frames (50, 40, 5);
            Assert.AreEqual (Ticks (1), budget.Budget);
            Assert.IsTrue (budget.AtFloor);
        }

        [Test]
        public void SlowFramesWithinTheFloorLeaveTheBudget ()
        {
            Frames (50, 40, 0.5);
            Assert.AreEqual (Ticks (10), budget.Budget);
        }

        [Test]
        public void GrowsWhileBoundWithinTheTarget ()
        {
            Frames (50, 40, 5);
            Frames (50, 16, 1, bound: true);
            Assert.AreEqual (Ticks (10), budget.Budget);
        }

        [Test]
        public void HoldsWhenNotBound ()
        {
            Frames (50, 40, 5);
            Frames (50, 16, 0.5);
            Assert.AreEqual (Ticks (1), budget.Budget);
        }

        [Test]
        public void IgnoresHeldFrames ()
        {
            for (int i = 0; i < 50; i++)
                Frame (500, 400, held: true);
            Assert.AreEqual (Ticks (10), budget.Budget);
        }

        [Test]
        public void FixedWithoutAdaptiveRateControl ()
        {
            budget.Configure (false, 60, 10000);
            Frames (50, 40, 10);
            Assert.AreEqual (Ticks (10), budget.Budget);
        }

        [Test]
        public void ChangedSettingsRestartTheBudget ()
        {
            Frames (50, 40, 5);
            budget.Configure (true, 60, 10000);
            Assert.AreEqual (Ticks (1), budget.Budget);
            budget.Configure (true, 30, 10000);
            Assert.AreEqual (Ticks (10), budget.Budget);
        }

        [Test]
        public void AllowanceIsWhatIsLeftAboveTheFloor ()
        {
            Assert.AreEqual (Ticks (6), budget.Allowance (Ticks (4)));
            Assert.AreEqual (Ticks (1), budget.Allowance (Ticks (9.5)));
            Assert.AreEqual (Ticks (1), budget.Allowance (Ticks (20)));
        }

        [Test]
        public void FloorIsAtMostTheMaximum ()
        {
            budget.Configure (false, 60, 500);
            Assert.AreEqual (Ticks (0.5), budget.Allowance (Ticks (20)));
        }

        [Test]
        public void MaximumIsOneSecond ()
        {
            budget.Configure (false, 60, 5000000);
            Assert.AreEqual (Ticks (1000), budget.Budget);
        }

        [Test]
        public void MaximumIsAtLeastATenthOfAMillisecond ()
        {
            budget.Configure (false, 60, 0);
            Assert.AreEqual (Ticks (0.1), budget.Budget);
            Assert.AreEqual (Ticks (0.1), budget.Allowance (Ticks (20)));
        }

        [Test]
        public void TargetIsFromOneToAThousandFramesASecond ()
        {
            // A target of zero is one frame a second, which a 1.5 s frame misses
            budget.Configure (true, 0, 10000);
            Frames (50, 1500, 5);
            Assert.AreEqual (Ticks (1), budget.Budget);
            // A target past a thousand is a thousand, which a 0.9 ms frame meets
            budget.Configure (true, 5000, 10000);
            Frames (50, 0.9, 0.5, bound: true);
            Assert.AreEqual (Ticks (10), budget.Budget);
        }
    }
}
