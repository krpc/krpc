using System;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [TestFixture]
    public class RoundRobinSchedulerTest
    {
        [Test]
        public void Empty ()
        {
            var s = new RoundRobinScheduler<int> ();
            Assert.IsTrue (s.Empty);
            Assert.Throws<InvalidOperationException> (() => s.Next ());
            Assert.Throws<InvalidOperationException> (() => s.Remove (0));
        }

        [Test]
        public void RemoveNonExistantClient ()
        {
            var s = new RoundRobinScheduler<int> ();
            s.Add (0);
            Assert.IsFalse (s.Empty);
            Assert.Throws<InvalidOperationException> (() => s.Remove (1));
        }

        [Test]
        public void AddExistingClient ()
        {
            var s = new RoundRobinScheduler<int> ();
            s.Add (0);
            Assert.IsFalse (s.Empty);
            Assert.Throws<InvalidOperationException> (() => s.Add (0));
        }

        [Test]
        public void Single ()
        {
            var s = new RoundRobinScheduler<int> ();
            for (int i = 0; i < 5; i++) {
                s.Add (0);
                Assert.IsFalse (s.Empty);
                Assert.AreEqual (0, s.Next ());
                Assert.AreEqual (0, s.Next ());
                Assert.AreEqual (0, s.Next ());
                s.Remove (0);
                Assert.IsTrue (s.Empty);
            }
        }

        [Test]
        public void SimpleCase ()
        {
            var s = new RoundRobinScheduler<int> ();
            s.Add (0);
            s.Add (1);
            s.Add (2);
            Assert.IsFalse (s.Empty);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual (0, s.Next ());
                Assert.AreEqual (1, s.Next ());
                Assert.AreEqual (2, s.Next ());
            }
            s.Remove (0);
            s.Remove (1);
            s.Remove (2);
            Assert.IsTrue (s.Empty);
        }

        [Test]
        public void RemoveDuring ()
        {
            var s = new RoundRobinScheduler<int> ();
            s.Add (0);
            s.Add (1);
            s.Add (2);
            Assert.AreEqual (0, s.Next ());
            Assert.AreEqual (1, s.Next ());
            Assert.AreEqual (2, s.Next ());
            Assert.AreEqual (0, s.Next ());
            s.Remove (1);
            Assert.IsFalse (s.Empty);
            Assert.AreEqual (2, s.Next ());
            Assert.AreEqual (0, s.Next ());
            Assert.AreEqual (2, s.Next ());
            s.Remove (0);
            Assert.IsFalse (s.Empty);
            Assert.AreEqual (2, s.Next ());
            Assert.AreEqual (2, s.Next ());
            Assert.AreEqual (2, s.Next ());
            s.Remove (2);
            Assert.IsTrue (s.Empty);
            Assert.Throws<InvalidOperationException> (() => s.Next ());
            Assert.Throws<InvalidOperationException> (() => s.Remove (0));
        }

        [Test]
        public void AddDuring ()
        {
            var s = new RoundRobinScheduler<int> ();
            s.Add (0);
            s.Add (1);
            Assert.AreEqual (0, s.Next ());
            Assert.AreEqual (1, s.Next ());
            Assert.AreEqual (0, s.Next ());
            Assert.AreEqual (1, s.Next ());
            s.Add (2);
            Assert.AreEqual (0, s.Next ());
            Assert.AreEqual (1, s.Next ());
            Assert.AreEqual (2, s.Next ());
            Assert.AreEqual (0, s.Next ());
            Assert.AreEqual (1, s.Next ());
            Assert.AreEqual (2, s.Next ());
        }
    }
}
