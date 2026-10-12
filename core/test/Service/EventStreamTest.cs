using System;
using KRPC.Service;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class EventStreamTest
    {
        [SetUp]
        public void SetUp ()
        {
            CallContext.GameScene = GameScene.Flight;
        }

        [Test]
        public void TriggersAndResets ()
        {
            var triggered = false;
            var stream = new EventStream (() => triggered);

            stream.UpdateInternal ();
            Assert.IsFalse (stream.Changed);

            triggered = true;
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.AreEqual (true, stream.Result.Value);

            stream.Sent ();
            Assert.IsFalse (stream.Changed);
            Assert.AreEqual (false, stream.Result.Value);
        }

        [Test]
        public void ErrorsAreCaptured ()
        {
            var stream = new EventStream (
                () => { throw new InvalidOperationException ("it went wrong"); });
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsFalse (stream.Result.HasValue);
            Assert.IsTrue (stream.Result.HasError);
            StringAssert.Contains ("it went wrong", stream.Result.Error.Description);
            stream.Sent ();
            Assert.IsFalse (stream.Changed);
            Assert.IsTrue (stream.Result.HasError);
        }

        [Test]
        public void TriggerClearsAnError ()
        {
            var fail = true;
            var stream = new EventStream (() => {
                if (fail)
                    throw new InvalidOperationException ("it went wrong");
                return true;
            });
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Result.HasError);
            stream.Sent ();
            fail = false;
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsFalse (stream.Result.HasError);
            Assert.AreEqual (true, stream.Result.Value);
        }
    }
}
