using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Messages;
using Moq;
using NUnit.Framework;
using Expression = KRPC.Service.KRPC.Expression;
using Type = KRPC.Service.KRPC.Type;

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

            // The first update sends that the event has not fired
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.AreEqual (false, stream.Result.Value);
            stream.Sent ();

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
        public void AFiringIsSentBeforeAnErrorInTheSameUpdate ()
        {
            var fail = false;
            var stream = new EventStream (() => {
                if (fail)
                    throw new InvalidOperationException ("it went wrong");
                return false;
            });
            stream.UpdateInternal ();
            stream.Sent ();
            fail = true;
            stream.Trigger ();
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.AreEqual (true, stream.Result.Value);
            stream.Sent ();
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsTrue (stream.Result.HasError);
            StringAssert.Contains ("it went wrong", stream.Result.Error.Description);
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

        [Test]
        public void AProcedureWithNoReturnValueCannotBeStreamed ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => new ProcedureCallStream (new ProcedureCall ("TestService", "ProcedureNoArgsNoReturn")));
            StringAssert.Contains ("does not return a value", exn.Message);
        }

        [Test]
        public void AProcedureReturningAStreamCannotBeStreamed ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => new ProcedureCallStream (new ProcedureCall ("KRPC", "AddStream")));
            StringAssert.Contains ("KRPC.AddStream returns a stream, and cannot be streamed", exn.Message);
        }

        [Test]
        public void AProcedureReturningAnEventCannotBeStreamed ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => new ProcedureCallStream (new ProcedureCall ("KRPC", "AddEvent")));
            StringAssert.Contains ("KRPC.AddEvent returns an event, and cannot be streamed", exn.Message);
        }

        [Test]
        public void AFunctionEventEndsOnAnObjectNotLoaded ()
        {
            var stream = new EventStream (
                () => { throw new global::KRPC.Service.KRPC.ObjectNotLoadedException ("not loaded"); },
                false);
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Result.HasError);
            StringAssert.Contains ("not loaded", stream.Result.Error.Description);
        }

        [Test]
        public void FunctionEvent ()
        {
            var stream = new EventStream (global::KRPC.Service.KRPC.KRPC.EventCondition (
                Expression.Equal (Expression.ConstantInt (1), Expression.ConstantInt (1))));
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.AreEqual (true, stream.Result.Value);
        }

        [Test]
        public void FunctionThatIsNotBooleanIsRejected ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => global::KRPC.Service.KRPC.KRPC.AddEvent (Expression.ConstantInt (1)));
            StringAssert.Contains ("must evaluate to a boolean value", exn.Message);
        }

        [Test]
        public void FunctionEvaluatingToNullIsAnError ()
        {
            var stream = new EventStream (global::KRPC.Service.KRPC.KRPC.EventCondition (
                Expression.ConstantNull (Type.Bool ())));
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsTrue (stream.Result.HasError);
            StringAssert.Contains ("evaluated to null", stream.Result.Error.Description);
        }

        [Test]
        public void YieldingProcedureIsReported ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()))
                .Returns ((int n, int sum) => {
                    throw new YieldException<Func<int>> (() => 0);
                });
            TestService.Service = mock.Object;
            var call = new ProcedureCall ("TestService", "BlockingProcedureReturns");
            call.Arguments.Add (new Argument (0, 1));
            var stream = new EventStream (global::KRPC.Service.KRPC.KRPC.EventCondition (
                Expression.Equal (Expression.Call (call), Expression.ConstantInt (0))));
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsTrue (stream.Result.HasError);
            StringAssert.Contains ("paused execution", stream.Result.Error.Description);
            Assert.IsNotNull (stream.Result.Error.StackTrace);
        }
    }
}
