using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using KRPC.Server;
using KRPC.Server.ProtocolBuffers;
using KRPC.Server.TCP;
using KRPC.Service;
using KRPC.Test.Server;
using KRPC.Test.Service;
using Moq;
using NUnit.Framework;
using ContentCase = KRPC.Schema.KRPC.Message.ContentOneofCase;
using SchemaMessage = KRPC.Schema.KRPC.Message;

namespace KRPC.Test
{
    /// <summary>
    /// Requests a client sends ahead of their responses, over a real connection with no
    /// client library.
    /// </summary>
    [TestFixture]
    public class PipeliningTest
    {
        Core core;
        KRPC.Server.Server server;
        TcpClient socket;
        NetworkStream stream;
        PinnedUpdateTime updateTime;
        readonly MemoryStream received = new MemoryStream ();

        static void Approve (object sender, ClientRequestingConnectionEventArgs args)
        {
            args.Request.Allow ();
        }

        [SetUp]
        public void SetUp ()
        {
            core = Core.Instance;
            CallContext.GameScene = GameScene.Flight;
            updateTime = new PinnedUpdateTime ();
            var tcpServer = new TCPServer (IPAddress.Loopback, 0);
            server = new KRPC.Server.Server (
                Guid.NewGuid (), Protocol.ProtocolBuffersOverTCP, "Pipelining",
                new KRPC.Server.Delimited.Server (tcpServer));
            core.OnClientRequestingConnection += Approve;
            core.Add (server);
            server.Start ();
            socket = new TcpClient ();
            socket.Connect (IPAddress.Loopback, tcpServer.ActualPort);
            stream = socket.GetStream ();
            Send (MessageHarness.ConnectionRequest (2, "Pipelining"));
            var response = Receive (1) [0];
            Assert.AreEqual (ContentCase.ConnectionResponse, response.ContentCase);
        }

        [TearDown]
        public void TearDown ()
        {
            socket.Close ();
            server.Stop ();
            core.Remove (server.Id);
            core.OnClientRequestingConnection -= Approve;
            updateTime.Restore ();
            TestService.Service = null;
        }

        void Send (SchemaMessage message)
        {
            message.WriteDelimitedTo (stream);
        }

        static SchemaMessage BlockingRequest (ulong id, int yields)
        {
            var message = MessageHarness.Request (id, "TestService", "BlockingProcedureReturns");
            message.Request.Calls [0].Arguments.Add (new Schema.KRPC.Argument {
                Position = 0,
                Value = Encoder.Encode (yields, TypeSpec.Create (typeof (int)))
            });
            return message;
        }

        /// <summary>
        /// Update the server until the client has received the given number of messages, and
        /// return them. Each entry of <paramref name="updates"/>, if given, is the number of
        /// updates run before the message at that index arrived.
        /// </summary>
        IList<SchemaMessage> Receive (int count, IList<int> updates = null)
        {
            var messages = new List<SchemaMessage> ();
            var timer = Stopwatch.StartNew ();
            var buffer = new byte [4096];
            var run = 0;
            while (messages.Count < count) {
                Assert.Less (timer.ElapsedMilliseconds, 10000, "Timed out waiting for messages");
                core.Update ();
                run++;
                while (socket.Available > 0) {
                    var read = stream.Read (buffer, 0, buffer.Length);
                    received.Write (buffer, 0, read);
                }
                received.Position = 0;
                while (received.Position < received.Length) {
                    var start = received.Position;
                    SchemaMessage message;
                    try {
                        message = SchemaMessage.Parser.ParseDelimitedFrom (received);
                    } catch (InvalidProtocolBufferException) {
                        received.Position = start;
                        break;
                    }
                    messages.Add (message);
                    if (updates != null)
                        updates.Add (run);
                }
                var rest = received.ToArray ();
                var left = (int)(received.Length - received.Position);
                received.SetLength (0);
                received.Write (rest, rest.Length - left, left);
            }
            return messages;
        }

        [Test]
        public void RequestsSentAheadRunInOrder ()
        {
            for (ulong id = 1; id <= 100; id++)
                Send (MessageHarness.Request (id, "KRPC", "GetClientName"));
            var responses = Receive (100);
            for (int i = 0; i < 100; i++) {
                Assert.AreEqual (ContentCase.Response, responses [i].ContentCase);
                Assert.AreEqual (i + 1, responses [i].Response.Id);
                Assert.IsNull (responses [i].Response.Results [0].Error);
            }
        }

        [Test]
        public void AYieldingCallHoldsUpTheRequestsBehindIt ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()))
                .Returns ((int n, int sum) => Blocking (n, sum));
            TestService.Service = mock.Object;
            Send (BlockingRequest (1, 5));
            Send (MessageHarness.Request (2, "KRPC", "GetClientName"));
            Send (MessageHarness.Request (3, "KRPC", "GetClientName"));
            var updates = new List<int> ();
            var responses = Receive (3, updates);
            CollectionAssert.AreEqual (new ulong[] { 1, 2, 3 }, new [] {
                responses [0].Response.Id, responses [1].Response.Id, responses [2].Response.Id });
            // The call yields on five updates, and completes on the sixth
            Assert.GreaterOrEqual (updates [0], 6);
        }

        [Test]
        public void AResponseHeldBackIsSentAtTheEndOfTheUpdate ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()))
                .Returns ((int n, int sum) => Blocking (n, sum));
            TestService.Service = mock.Object;
            // The first response is held back while the second request is queued, and the
            // second request yields until its sixth update
            SendAhead (MessageHarness.Request (1, "KRPC", "GetClientName"), BlockingRequest (2, 5));
            var updates = new List<int> ();
            var responses = Receive (2, updates);
            Assert.AreEqual (1, responses [0].Response.Id);
            Assert.AreEqual (1, updates [0]);
            Assert.GreaterOrEqual (updates [1], 6);
        }

        [Test]
        public void QueuedRequestsAreDroppedWhenTheClientDisconnects ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()))
                .Returns ((int n, int sum) => Blocking (n, sum));
            var executed = 0;
            mock.Setup (x => x.ProcedureNoArgsNoReturn ()).Callback (() => executed++);
            TestService.Service = mock.Object;
            // The requests behind the yielding call would run once it completes, on its
            // eleventh update
            Send (BlockingRequest (1, 10));
            for (ulong id = 2; id <= 10; id++)
                Send (MessageHarness.Request (id));
            for (int i = 0; i < 5; i++)
                core.Update ();
            socket.Close ();
            for (int i = 0; i < 50; i++)
                core.Update ();
            Assert.AreEqual (0, executed);
        }

        static SchemaMessage EchoListRequest (ulong id, params string[] values)
        {
            var message = MessageHarness.Request (id, "TestService", "EchoList");
            message.Request.Calls [0].Arguments.Add (new Schema.KRPC.Argument {
                Position = 0,
                Value = Encoder.Encode (new List<string> (values), TypeSpec.Create (typeof (IList<string>)))
            });
            return message;
        }

        // Write the messages in one send, and wait for the server to read them, so that each
        // is read ahead of the ones before it running
        void SendAhead (params SchemaMessage[] messages)
        {
            var buffer = new MemoryStream ();
            foreach (var message in messages)
                message.WriteDelimitedTo (buffer);
            var expected = server.BytesRead + (ulong)buffer.Length;
            stream.Write (buffer.ToArray (), 0, (int)buffer.Length);
            var timer = Stopwatch.StartNew ();
            while (server.BytesRead < expected) {
                Assert.Less (timer.ElapsedMilliseconds, 10000, "Timed out waiting for the server to read");
                System.Threading.Thread.Sleep (1);
            }
        }

        [Test]
        public void ADecodingErrorIsAnsweredInOrder ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoList (It.IsAny<IList<string>> ()))
                .Returns ((IList<string> x) => x);
            TestService.Service = mock.Object;
            var undecodable = MessageHarness.Request (2, "TestService", "BlockingProcedureNoReturn");
            undecodable.Request.Calls [0].Arguments.Add (new Schema.KRPC.Argument {
                Position = 0,
                Value = ByteString.CopyFrom (0xff)
            });
            SendAhead (EchoListRequest (1, "a", "b"), undecodable, EchoListRequest (3, "c"));
            var responses = Receive (3);
            CollectionAssert.AreEqual (new ulong[] { 1, 2, 3 }, new [] {
                responses [0].Response.Id, responses [1].Response.Id, responses [2].Response.Id });
            var listSpec = TypeSpec.Create (typeof (IList<string>));
            CollectionAssert.AreEqual (new [] { "a", "b" },
                (IList<string>)Encoder.Decode (responses [0].Response.Results [0].Value, listSpec));
            Assert.AreEqual ("ArgumentException", responses [1].Response.Results [0].Error.Exception);
            CollectionAssert.AreEqual (new [] { "c" },
                (IList<string>)Encoder.Decode (responses [2].Response.Results [0].Value, listSpec));
        }

        [Test]
        public void AnObjectArgumentResolvesWhenItsCallRuns ()
        {
            var obj = new TestService.TestClass ("removed");
            var id = ObjectStore.Instance.AddInstance (obj);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.DeleteTestObject (It.IsAny<TestService.TestClass> ()))
                .Callback ((TestService.TestClass x) => ObjectStore.Instance.RemoveInstance (x));
            mock.Setup (x => x.EchoTestObject (It.IsAny<TestService.TestClass> ()))
                .Returns ((TestService.TestClass x) => x);
            TestService.Service = mock.Object;
            var argument = Encoder.Encode (obj, TypeSpec.Create (typeof (TestService.TestClass)));
            var delete = MessageHarness.Request (1, "TestService", "DeleteTestObject");
            delete.Request.Calls [0].Arguments.Add (new Schema.KRPC.Argument { Position = 0, Value = argument });
            var echo = MessageHarness.Request (2, "TestService", "EchoTestObject");
            echo.Request.Calls [0].Arguments.Add (new Schema.KRPC.Argument { Position = 0, Value = argument });
            SendAhead (delete, echo);
            var responses = Receive (2);
            Assert.IsNull (responses [0].Response.Results [0].Error);
            var error = responses [1].Response.Results [0].Error;
            Assert.IsNotNull (error);
            Assert.AreEqual ("ObjectDestroyedException", error.Exception);
        }

        // Yields until n reaches zero, then returns the sum of the values of n
        static int Blocking (int n, int sum)
        {
            if (n == 0)
                return sum;
            throw new YieldException<Func<int>> (() => Blocking (n - 1, sum + n));
        }
    }
}
