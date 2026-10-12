using System;
using System.Collections.Generic;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Test.Service;
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

        sealed class RecordingStream : global::KRPC.Server.Message.StreamStream
        {
            public RecordingStream () : base (null)
            {
            }

            public List<List<SentResult>> Updates { get; } = new List<List<SentResult>> ();

            public List<ulong> RemovedIds { get; } = new List<ulong> ();

            public override void Write (StreamUpdate value)
            {
                var update = new List<SentResult> ();
                foreach (var result in value.Results)
                    update.Add (new SentResult {
                        Id = result.Id,
                        HasValue = result.Result.HasValue,
                        Value = result.Result.Value,
                        Error = result.Result.HasError ? result.Result.Error.Description : null,
                        ErrorName = result.Result.HasError ? result.Result.Error.Name : null
                    });
                Updates.Add (update);
            }

            public override void Removed (ulong id)
            {
                RemovedIds.Add (id);
            }
        }

        sealed class RecordingClient : IClient<NoMessage,StreamUpdate>
        {
            public RecordingClient (Guid guid)
            {
                Guid = guid;
                Stream = new RecordingStream ();
            }

            public string Name { get { return "recording"; } }

            public Guid Guid { get; private set; }

            public string Address { get { return "recording"; } }

            public bool Connected { get { return true; } }

            public IStream<NoMessage,StreamUpdate> Stream { get; private set; }

            public RecordingStream Recorded { get { return (RecordingStream)Stream; } }

            public void Close ()
            {
            }

            public bool Equals (IClient<NoMessage,StreamUpdate> other)
            {
                return other != null && Guid == other.Guid;
            }

            public override bool Equals (object obj)
            {
                return Equals (obj as IClient<NoMessage,StreamUpdate>);
            }

            public override int GetHashCode ()
            {
                return Guid.GetHashCode ();
            }
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
        ScriptedClient rpcClient;
        RecordingClient streamClient;

        [SetUp]
        public void SetUp ()
        {
            core = Core.Instance;
            CallContext.GameScene = GameScene.Flight;
            blockingRecv = Configuration.Instance.BlockingRecv;
            Configuration.Instance.BlockingRecv = false;
            rpcClient = new ScriptedClient (0, 0);
            streamClient = new RecordingClient (rpcClient.Guid);
            core.RPCClientConnected (rpcClient);
            core.StreamClientConnected (streamClient);
        }

        [TearDown]
        public void TearDown ()
        {
            core.StreamClientDisconnected (streamClient);
            core.RPCClientDisconnected (rpcClient);
            Configuration.Instance.BlockingRecv = blockingRecv;
        }

        ulong Add (global::KRPC.Service.Stream stream)
        {
            return core.AddStream (rpcClient, stream);
        }

        List<SentResult> Update ()
        {
            var updates = streamClient.Recorded.Updates;
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
        public void AThrowingStreamFailsOnlyItself ()
        {
            var failing = Add (new ThrowingStream ());
            var working = Add (new ConstantStream (42));

            var update = Update ();
            Assert.AreEqual (2, update.Count);
            StringAssert.Contains ("the stream failed", ResultFor (update, failing).Error);
            Assert.AreEqual (42, ResultFor (update, working).Value);

            // The failed stream is removed once its error is sent
            Assert.AreEqual (new List<ulong> { failing }, streamClient.Recorded.RemovedIds);
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
            Assert.AreEqual (new List<ulong> { failing }, streamClient.Recorded.RemovedIds);
        }

        [Test]
        public void ARateSetBeforeTheFirstUpdateDoesNotDelayIt ()
        {
            var id = Add (new ConstantStream (42));
            core.SetStreamRate (rpcClient, id, 0.1f);
            var update = Update ();
            Assert.AreEqual (1, update.Count);
            Assert.AreEqual (42, ResultFor (update, id).Value);
        }

        [Test]
        public void ManagementCallsOnAnEndedStreamDoNothing ()
        {
            var id = Add (new ConstantStream (42));
            core.RemoveStream (rpcClient, id);
            Update ();
            Assert.AreEqual (new List<ulong> { id }, streamClient.Recorded.RemovedIds);
            core.RemoveStream (rpcClient, id);
            core.SetStreamRate (rpcClient, id, 1);
            Assert.AreEqual (0, Update ().Count);
        }

        [Test]
        public void ManagementCallsOnAnIdNeverIssuedThrow ()
        {
            var id = Add (new ConstantStream (42));
            Assert.Throws<ArgumentException> (() => core.RemoveStream (rpcClient, id + 1));
            Assert.Throws<ArgumentException> (() => core.SetStreamRate (rpcClient, id + 1, 1));
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
            Assert.AreEqual (new List<ulong> { id }, streamClient.Recorded.RemovedIds);
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
            Assert.IsEmpty (streamClient.Recorded.RemovedIds);
            update = Update ();
            Assert.AreEqual ("StreamRemovedException", ResultFor (update, id).ErrorName);
            Assert.AreEqual (new List<ulong> { id }, streamClient.Recorded.RemovedIds);
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
            Assert.AreEqual (new List<ulong> { id }, streamClient.Recorded.RemovedIds);
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
            core.RemoveStream (rpcClient, first);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Returns ("bar");
            update = Update ();
            Assert.AreEqual (1, update.Count);
            Assert.AreEqual ("bar", ResultFor (update, second).Value);
        }
    }
}
