using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test
{
    [TestFixture]
    public class CallWaitTest
    {
        static readonly long budget = Ticks (10);
        static readonly long maxCallWait = Ticks (5);

        CallWait callWait;
        long now;

        [SetUp]
        public void SetUp ()
        {
            callWait = new CallWait ();
            now = Ticks (1000);
        }

        static long Ticks (double milliseconds)
        {
            return StopwatchExtensions.MicrosecondsToTicks ((long)(milliseconds * 1000));
        }

        // Record turnarounds of the given lengths, in milliseconds, in turn
        void Turnarounds (params double[] milliseconds)
        {
            for (int i = 0; i < CallWait.WindowSize; i++) {
                callWait.ResponseSent (now);
                now += Ticks (milliseconds [i % milliseconds.Length]);
                callWait.RequestArrived (now);
                now += Ticks (0.1);
            }
        }

        long Deadline (long at, long budgetLeft, long maxWait)
        {
            bool limited;
            return callWait.Deadline (at, budgetLeft, maxWait, out limited);
        }

        // The wait for the next call after a response sent now, in milliseconds, or zero
        double Wait ()
        {
            callWait.ResponseSent (now);
            var deadline = Deadline (now, budget, maxCallWait);
            return deadline == 0 ? 0 : (deadline - now) * 1000d / System.Diagnostics.Stopwatch.Frequency;
        }

        [Test]
        public void NoWaitWithNoResponseSent ()
        {
            Assert.AreEqual (0, Deadline (now, budget, maxCallWait));
        }

        [Test]
        public void NewClientWaitsAboutAMillisecond ()
        {
            Assert.That (Wait (), Is.InRange (1.3, 1.5));
        }

        [Test]
        public void NoWaitWithNoMaxCallWait ()
        {
            Turnarounds (0.1);
            callWait.ResponseSent (now);
            Assert.AreEqual (0, Deadline (now, budget, 0));
        }

        [Test]
        public void SynchronousClientInReach ()
        {
            Turnarounds (4);
            Assert.That (Wait (), Is.InRange (4, 5));
        }

        [Test]
        public void SynchronousClientOutOfReach ()
        {
            Turnarounds (8);
            Assert.AreEqual (0, Wait ());
        }

        [Test]
        public void BurstsOfTwo ()
        {
            Turnarounds (0.5, 19);
            Assert.That (Wait (), Is.InRange (1, 1.5));
        }

        [Test]
        public void BurstsOfThree ()
        {
            Turnarounds (2, 2, 16);
            Assert.That (Wait (), Is.InRange (2.5, 3));
        }

        [Test]
        public void OneCallPerTick ()
        {
            Turnarounds (20);
            Assert.AreEqual (0, Wait ());
        }

        [Test]
        public void RareQuickCall ()
        {
            Turnarounds (0.5, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20);
            Assert.AreEqual (0, Wait ());
        }

        [Test]
        public void WaitEndsAtItsDeadline ()
        {
            Turnarounds (1);
            callWait.ResponseSent (now);
            var deadline = Deadline (now, budget, maxCallWait);
            Assert.AreNotEqual (0, deadline);
            Assert.AreEqual (deadline, Deadline (now + Ticks (0.5), budget, maxCallWait));
            Assert.AreEqual (0, Deadline (deadline, budget, maxCallWait));
            Assert.AreEqual (0, Deadline (deadline + Ticks (1), budget, maxCallWait));
        }

        [Test]
        public void NoWaitPastEveryTurnaround ()
        {
            Turnarounds (1);
            callWait.ResponseSent (now);
            Assert.AreEqual (0, Deadline (now + Ticks (2), budget, maxCallWait));
        }

        [Test]
        public void NoWaitBeyondTheBudget ()
        {
            Turnarounds (4);
            callWait.ResponseSent (now);
            Assert.AreEqual (0, Deadline (now, Ticks (2), maxCallWait));
        }

        [Test]
        public void BudgetLimitedWait ()
        {
            Turnarounds (4);
            callWait.ResponseSent (now);
            bool limited;
            Assert.AreEqual (0, callWait.Deadline (now, Ticks (2), maxCallWait, out limited));
            Assert.IsTrue (limited);
            // Only the update that decides the wait is limited by the budget
            Assert.AreEqual (0, callWait.Deadline (now + Ticks (20), Ticks (2), maxCallWait, out limited));
            Assert.IsFalse (limited);
            Turnarounds (8);
            callWait.ResponseSent (now);
            Assert.AreEqual (0, callWait.Deadline (now, Ticks (2), maxCallWait, out limited));
            Assert.IsFalse (limited);
        }

        [Test]
        public void RequestsSentAheadAreNotTurnarounds ()
        {
            // A pipelining client: three requests sent ahead of each response, then a 2 ms gap
            for (int i = 0; i < CallWait.WindowSize; i++) {
                for (int j = 0; j < 3; j++) {
                    callWait.ResponseSent (now);
                    callWait.RequestArrived (now - Ticks (0.1));
                    now += Ticks (0.05);
                }
                callWait.ResponseSent (now);
                now += Ticks (2);
                callWait.RequestArrived (now);
            }
            Assert.That (Wait (), Is.InRange (2, 3));
        }

        [Test]
        public void NoWaitWhileACallIsOutstanding ()
        {
            Turnarounds (1);
            callWait.ResponseSent (now);
            callWait.RequestArrived (now + Ticks (1));
            Assert.AreEqual (0, Deadline (now + Ticks (1), budget, maxCallWait));
        }
    }
}
