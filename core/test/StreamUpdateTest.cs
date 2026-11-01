using System;
using System.Collections.Generic;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Test.Service;
using KRPC.Utils;
using Moq;
using NUnit.Framework;

namespace KRPC.Test
{
    [TestFixture]
    public class StreamUpdateTest
    {
        // A copy of a result, as the server reuses its result objects across updates
        sealed class SentResult
        {
            public ulong Id;
            public bool HasValue;
            public object Value;
            public string Error;
            public string ErrorName;
        }

        sealed class ConstantStream : global::KRPC.Service.Stream
        {
            readonly int value;

            public ConstantStream (int value)
            {
                this.value = value;
            }

            public override void UpdateInternal ()
            {
                var result = new ProcedureResult ();
                result.Spec = TypeSpec.Create (typeof (int));
                result.Value = value;
                Result = result;
            }
        }

        sealed class ThrowingStream : global::KRPC.Service.Stream
        {
            public override void UpdateInternal ()
            {
                throw new InvalidOperationException ("the stream failed");
            }
        }

        Core core;
        bool blockingRecv;
        PinnedUpdateTime updateTime;
        ScriptedClient client;
        List<List<SentResult>> updates;

        [SetUp]
        public void SetUp ()
        {
            core = Core.Instance;
            CallContext.GameScene = GameScene.Flight;
            blockingRecv = Configuration.Instance.BlockingRecv;
            Configuration.Instance.BlockingRecv = false;
            updateTime = new PinnedUpdateTime ();
            client = new ScriptedClient (0, 0);
            updates = new List<List<SentResult>> ();
            client.StreamUpdateWritten = value => {
                var update = new List<SentResult> ();
                foreach (var result in value.Results)
                    update.Add (new SentResult {
                        Id = result.Id,
                        HasValue = result.Result.HasValue,
                        Value = result.Result.Value,
                        Error = result.Result.HasError ? result.Result.Error.Description : null,
                        ErrorName = result.Result.HasError ? result.Result.Error.Name : null
                    });
                updates.Add (update);
            };
            core.ClientConnected (client);
        }

        [TearDown]
        public void TearDown ()
        {
            core.ClientDisconnected (client);
            Configuration.Instance.BlockingRecv = blockingRecv;
            updateTime.Restore ();
        }

        ulong Add (global::KRPC.Service.Stream stream)
        {
            return core.AddStream (client, stream);
        }

        List<SentResult> Update ()
        {
            var count = updates.Count;
            core.Update ();
            Assert.LessOrEqual (updates.Count, count + 1);
            return updates.Count > count ? updates [count] : new List<SentResult> ();
        }

        static SentResult ResultFor (List<SentResult> update, ulong id)
        {
            return update.Find (x => x.Id == id);
        }

        [Test]
        public void OnlyAChangedValueSendsAnUpdate ()
        {
            var sent = core.StreamUpdatesSent;
            Add (new ConstantStream (42));
            Assert.AreEqual (1, Update ().Count);
            Assert.AreEqual (0, Update ().Count);
            Assert.AreEqual (sent + 1, core.StreamUpdatesSent);
        }

        [Test]
        public void AThrowingStreamFailsOnlyItself ()
        {
            var failing = Add (new ThrowingStream ());
            var working = Add (new ConstantStream (42));

            var update = Update ();
            Assert.AreEqual (2, update.Count);
            StringAssert.Contains ("the stream failed", ResultFor (update, failing).Error);
            Assert.AreEqual (42, ResultFor (update, working).Value);

            // The failed stream is removed once its error is sent
            Assert.AreEqual (new List<ulong> { failing }, client.RemovedStreams);
            Assert.AreEqual (0, Update ().Count);
        }

        [Test]
        public void AnEncodingFailureFailsOnlyItsStream ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoListOfObjects (It.IsAny<IList<TestService.TestClass>> ()))
                .Returns (new List<TestService.TestClass> { null });
            TestService.Service = mock.Object;
            var call = new ProcedureCall ("TestService", "EchoListOfObjects");
            call.Arguments.Add (new Argument (0, new List<TestService.TestClass> ()));
            var failing = Add (new ProcedureCallStream (call));
            var working = Add (new ConstantStream (42));

            var update = Update ();
            Assert.AreEqual (2, update.Count);
            Assert.IsNotNull (ResultFor (update, failing).Error);
            Assert.AreEqual (42, ResultFor (update, working).Value);
            Assert.AreEqual (new List<ulong> { failing }, client.RemovedStreams);
        }

        [Test]
        public void AChangeToNegativeZeroIsSent ()
        {
            double? value = 0.0;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoNullableDouble (It.IsAny<double?> ())).Returns (() => value);
            TestService.Service = mock.Object;
            var call = new ProcedureCall ("TestService", "EchoNullableDouble");
            call.Arguments.Add (new Argument (0, 0.0));
            var id = Add (new ProcedureCallStream (call));

            Assert.AreEqual (0.0, ResultFor (Update (), id).Value);
            value = -0.0;
            var sent = (double)ResultFor (Update (), id).Value;
            Assert.Less (BitConverter.DoubleToInt64Bits (sent), 0);
            Assert.AreEqual (0, Update ().Count);
        }

        [Test]
        public void AListChangedInPlaceIsSent ()
        {
            var list = new List<string> { "a" };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoList (It.IsAny<IList<string>> ())).Returns (list);
            TestService.Service = mock.Object;
            var call = new ProcedureCall ("TestService", "EchoList");
            call.Arguments.Add (new Argument (0, new List<string> ()));
            var id = Add (new ProcedureCallStream (call));

            Assert.AreEqual (new List<string> { "a" }, ResultFor (Update (), id).Value);
            Assert.AreEqual (0, Update ().Count);
            list.Add ("b");
            Assert.AreEqual (new List<string> { "a", "b" }, ResultFor (Update (), id).Value);
        }

        [Test]
        public void AReorderedSetIsNotSent ()
        {
            var calls = 0;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoSet (It.IsAny<HashSet<int>> ()))
                .Returns (() => ++calls % 2 == 1 ? new HashSet<int> { 1, 2, 3 } : new HashSet<int> { 3, 1, 2 });
            TestService.Service = mock.Object;
            var call = new ProcedureCall ("TestService", "EchoSet");
            call.Arguments.Add (new Argument (0, new HashSet<int> ()));
            var id = Add (new ProcedureCallStream (call));

            CollectionAssert.AreEquivalent (new [] { 1, 2, 3 }, (HashSet<int>)ResultFor (Update (), id).Value);
            Assert.AreEqual (0, Update ().Count);
            Assert.AreEqual (2, calls);
        }

        [Test]
        public void AReorderedDictionaryIsNotSent ()
        {
            var calls = 0;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoDictionary (It.IsAny<IDictionary<int,string>> ()))
                .Returns (() => ++calls % 2 == 1 ?
                    new Dictionary<int,string> { { 1, "a" }, { 2, "b" } } :
                    new Dictionary<int,string> { { 2, "b" }, { 1, "a" } });
            TestService.Service = mock.Object;
            var call = new ProcedureCall ("TestService", "EchoDictionary");
            call.Arguments.Add (new Argument (0, new Dictionary<int,string> ()));
            var id = Add (new ProcedureCallStream (call));

            Assert.AreEqual (2, ((IDictionary<int,string>)ResultFor (Update (), id).Value).Count);
            Assert.AreEqual (0, Update ().Count);
            Assert.AreEqual (2, calls);
        }

        [Test]
        public void ARateSetBeforeTheFirstUpdateDoesNotDelayIt ()
        {
            var id = Add (new ConstantStream (42));
            core.SetStreamRate (client, id, 0.1f);
            var update = Update ();
            Assert.AreEqual (1, update.Count);
            Assert.AreEqual (42, ResultFor (update, id).Value);
        }

        [Test]
        public void ManagementCallsOnAnEndedStreamDoNothing ()
        {
            var id = Add (new ConstantStream (42));
            core.RemoveStream (client, id);
            Update ();
            Assert.AreEqual (new List<ulong> { id }, client.RemovedStreams);
            core.RemoveStream (client, id);
            core.SetStreamRate (client, id, 1);
            Assert.AreEqual (0, Update ().Count);
        }

        [Test]
        public void ManagementCallsOnAnIdNeverIssuedThrow ()
        {
            var id = Add (new ConstantStream (42));
            Assert.Throws<ArgumentException> (() => core.RemoveStream (client, id + 1));
            Assert.Throws<ArgumentException> (() => core.SetStreamRate (client, id + 1, 1));
        }

        [Test]
        public void AnEventEndedWithoutAFiringSendsStreamRemoved ()
        {
            var evnt = new EventStream ();
            var id = Add (evnt);
            evnt.Remove ();
            var update = Update ();
            Assert.AreEqual (1, update.Count);
            Assert.AreEqual ("StreamRemovedException", ResultFor (update, id).ErrorName);
            Assert.AreEqual (new List<ulong> { id }, client.RemovedStreams);
        }

        [Test]
        public void AnEventThatFiresAndEndsSendsTheFiringFirst ()
        {
            var evnt = new EventStream ();
            var id = Add (evnt);
            evnt.Trigger ();
            evnt.Remove ();
            var update = Update ();
            Assert.AreEqual (true, ResultFor (update, id).Value);
            Assert.IsEmpty (client.RemovedStreams);
            update = Update ();
            Assert.AreEqual ("StreamRemovedException", ResultFor (update, id).ErrorName);
            Assert.AreEqual (new List<ulong> { id }, client.RemovedStreams);
        }

        [Test]
        public void AnEventThatFiresAndEndsInOneUpdateSendsTheFiringFirst ()
        {
            EventStream evnt = null;
            evnt = new EventStream (() => {
                evnt.Remove ();
                return true;
            });
            var id = Add (evnt);
            Assert.AreEqual (true, ResultFor (Update (), id).Value);
            Assert.AreEqual ("StreamRemovedException", ResultFor (Update (), id).ErrorName);
            Assert.AreEqual (new List<ulong> { id }, client.RemovedStreams);
        }

        [Test]
        public void AnEventSendsThatItHasNotFiredOnItsFirstUpdate ()
        {
            var id = Add (new EventStream ());
            Assert.AreEqual (false, ResultFor (Update (), id).Value);
            Assert.AreEqual (0, Update ().Count);
        }

        [Test]
        public void EqualStreamsAreEvaluatedAndSentSeparately ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Returns ("foo");
            TestService.Service = mock.Object;
            var call = new ProcedureCall ("TestService", "ProcedureNoArgsReturns");
            var first = Add (new ProcedureCallStream (call));
            var second = Add (new ProcedureCallStream (call));
            Assert.AreNotEqual (first, second);

            var update = Update ();
            Assert.AreEqual ("foo", ResultFor (update, first).Value);
            Assert.AreEqual ("foo", ResultFor (update, second).Value);
            mock.Verify (x => x.ProcedureNoArgsReturns (), Times.Exactly (2));

            // Removing one leaves the other
            core.RemoveStream (client, first);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Returns ("bar");
            update = Update ();
            Assert.AreEqual (1, update.Count);
            Assert.AreEqual ("bar", ResultFor (update, second).Value);
        }

        [Test]
        public void AStreamOverAnObjectNotLoadedKeepsItsValue ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Returns ("foo");
            TestService.Service = mock.Object;
            var id = Add (new ProcedureCallStream (new ProcedureCall ("TestService", "ProcedureNoArgsReturns")));
            Assert.AreEqual ("foo", ResultFor (Update (), id).Value);

            mock.Setup (x => x.ProcedureNoArgsReturns ())
                .Throws (new global::KRPC.Service.KRPC.ObjectNotLoadedException ("not loaded"));
            Assert.AreEqual (0, Update ().Count);
            Assert.AreEqual (0, Update ().Count);
            Assert.IsEmpty (client.RemovedStreams);

            // The value it holds is the one sent, so an equal value is not sent again
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Returns ("foo");
            Assert.AreEqual (0, Update ().Count);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Returns ("bar");
            Assert.AreEqual ("bar", ResultFor (Update (), id).Value);
        }

        [Test]
        public void AStreamOverAProcedureNotAvailableInTheSceneIsHeld ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.PropertyAvailableInInheritedGameScene).Returns ("foo");
            TestService.Service = mock.Object;
            CallContext.GameScene = GameScene.SpaceCenter;
            var id = Add (new ProcedureCallStream (
                new ProcedureCall ("TestService", "get_PropertyAvailableInInheritedGameScene")));
            Assert.AreEqual (0, Update ().Count);
            Assert.AreEqual (0, Update ().Count);
            Assert.IsEmpty (client.RemovedStreams);

            CallContext.GameScene = GameScene.Flight;
            Assert.AreEqual ("foo", ResultFor (Update (), id).Value);
        }

        [Test]
        public void AStreamHeldInAContinuationRunsTheCallAgain ()
        {
            var loaded = false;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()))
                .Returns ((int n, int sum) => {
                    throw new YieldException<Func<int>> (() => {
                        if (!loaded)
                            throw new global::KRPC.Service.KRPC.ObjectNotLoadedException ("not loaded");
                        return 42;
                    });
                });
            TestService.Service = mock.Object;
            var call = new ProcedureCall ("TestService", "BlockingProcedureReturns");
            call.Arguments.Add (new Argument (0, 1));
            var id = Add (new ProcedureCallStream (call));
            for (var i = 0; i < 4; i++)
                Assert.AreEqual (0, Update ().Count);
            Assert.IsEmpty (client.RemovedStreams);

            loaded = true;
            SentResult result = null;
            for (var i = 0; i < 4 && result == null; i++) {
                var update = Update ();
                if (update.Count > 0)
                    result = ResultFor (update, id);
            }
            Assert.IsNotNull (result);
            Assert.AreEqual (42, result.Value);
            // Each hold runs the call from the start
            mock.Verify (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()), Times.AtLeast (3));
        }

        [Test]
        public void AHeldStreamIsNotLogged ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ())
                .Throws (new global::KRPC.Service.KRPC.ObjectNotLoadedException ("not loaded"));
            TestService.Service = mock.Object;
            var writer = Logger.Writer;
            var level = Logger.Level;
            var messages = new List<string> ();
            try {
                Logger.Level = Logger.Severity.Debug;
                Logger.Writer = (message, severity) => messages.Add (message);
                Add (new ProcedureCallStream (new ProcedureCall ("TestService", "ProcedureNoArgsReturns")));
                messages.Clear ();
                Update ();
                Update ();
            } finally {
                Logger.Writer = writer;
                Logger.Level = level;
            }
            Assert.IsEmpty (messages);
        }

        [Test]
        public void AnEventOverAnObjectNotLoadedHolds ()
        {
            var loaded = false;
            var fired = false;
            var id = Add (new EventStream (() => {
                if (!loaded)
                    throw new global::KRPC.Service.KRPC.ObjectNotLoadedException ("not loaded");
                return fired;
            }));
            // The first update sends that the event has not fired, and nothing follows
            Assert.AreEqual (false, ResultFor (Update (), id).Value);
            Assert.AreEqual (0, Update ().Count);
            loaded = true;
            fired = true;
            Assert.AreEqual (true, ResultFor (Update (), id).Value);
            Assert.IsEmpty (client.RemovedStreams);
        }

        [Test]
        public void AnEventOverAProcedureNotAvailableInTheSceneHolds ()
        {
            var available = false;
            var id = Add (new EventStream (() => {
                if (!available)
                    throw new global::KRPC.Service.KRPC.UnsupportedSceneException ("not available");
                return true;
            }));
            Assert.AreEqual (false, ResultFor (Update (), id).Value);
            Assert.AreEqual (0, Update ().Count);
            available = true;
            Assert.AreEqual (true, ResultFor (Update (), id).Value);
            Assert.IsEmpty (client.RemovedStreams);
        }
    }
}
