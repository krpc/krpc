using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Google.Protobuf;
using KRPC.Server;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;
using KRPC.Utils;
using NUnit.Framework;
using ContentCase = KRPC.Schema.KRPC.Message.ContentOneofCase;
using Reason = KRPC.Schema.KRPC.Disconnect.Types.Reason;
using SchemaMessage = KRPC.Schema.KRPC.Message;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;

namespace KRPC.Test.Server.ProtocolBuffers
{
    [TestFixture]
    public class ServerTest
    {
        static MessageHarness Harness ()
        {
            return new MessageHarness (byteServer => new KRPC.Server.ProtocolBuffers.Server (byteServer));
        }

        static void CheckRefused (MessageHarness harness, Status status, string message = null)
        {
            var response = harness.ReceiveOne ();
            Assert.AreEqual (ContentCase.ConnectionResponse, response.ContentCase);
            Assert.AreEqual (status, response.ConnectionResponse.Status);
            if (message != null)
                Assert.AreEqual (message, response.ConnectionResponse.Message);
            Assert.AreEqual (0, harness.Server.Clients.Count ());
            Assert.AreEqual (0, harness.Connected.Count);
            Assert.IsTrue (harness.Stream.HalfClosed);
        }

        static void CheckDisconnect (MessageHarness harness, Reason reason)
        {
            var message = harness.ReceiveOne ();
            Assert.AreEqual (ContentCase.Disconnect, message.ContentCase);
            Assert.AreEqual (reason, message.Disconnect.Reason);
            Assert.IsTrue (harness.Stream.HalfClosed);
        }

        [Test]
        public void Handshake ()
        {
            var harness = Harness ();
            var client = harness.Handshake ("Jebediah Kerman!!!");
            Assert.AreEqual (1, harness.Server.Clients.Count ());
            Assert.AreEqual ("Jebediah Kerman!!!", client.Name);
            Assert.IsTrue (client.Connected);
            Assert.IsFalse (harness.Stream.HalfClosed);
        }

        [Test]
        public void HandshakeResponseCarriesTheProtocolVersion ()
        {
            var harness = Harness ();
            harness.Send (MessageHarness.ConnectionRequest (2));
            harness.Server.Update ();
            var response = harness.ReceiveOne ().ConnectionResponse;
            Assert.AreEqual (Status.Ok, response.Status);
            Assert.AreEqual (2u, response.ProtocolVersion);
            Assert.AreEqual (16, response.ClientIdentifier.Length);
        }

        [Test]
        public void HandshakeInParts ()
        {
            var harness = Harness ();
            var bytes = MessageHarness.ConnectionRequest (2).ToByteArray ();
            var framed = new byte [bytes.Length + 1];
            framed [0] = (byte)bytes.Length;
            Array.Copy (bytes, 0, framed, 1, bytes.Length);
            harness.SendBytes (framed.Take (5).ToArray ());
            harness.Server.Update ();
            Assert.AreEqual (0, harness.Receive ().Count);
            harness.SendBytes (framed.Skip (5).ToArray ());
            harness.Server.Update ();
            Assert.AreEqual (Status.Ok, harness.ReceiveOne ().ConnectionResponse.Status);
            Assert.AreEqual (1, harness.Server.Clients.Count ());
        }

        [Test]
        public void UnsupportedProtocolVersion ()
        {
            var harness = Harness ();
            harness.Send (MessageHarness.ConnectionRequest (1));
            harness.Server.Update ();
            CheckRefused (harness, Status.WrongProtocolVersion,
                "Protocol version 1 is not supported. The server supports protocol version 2.");
        }

        [Test]
        public void NoProtocolVersion ()
        {
            var harness = Harness ();
            harness.Send (MessageHarness.ConnectionRequest (0));
            harness.Server.Update ();
            CheckRefused (harness, Status.WrongProtocolVersion,
                "The client sent no protocol version. The server supports protocol version 2.");
        }

        [Test]
        public void MalformedFirstMessage ()
        {
            var harness = Harness ();
            harness.SendBytes (new byte[] { 0x04, 0xff, 0xff, 0xff, 0xff });
            harness.Server.Update ();
            CheckRefused (harness, Status.MalformedMessage);
        }

        [Test]
        public void InvalidLengthPrefix ()
        {
            var harness = Harness ();
            harness.SendBytes (new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff });
            harness.Server.Update ();
            CheckRefused (harness, Status.MalformedMessage);
        }

        [Test]
        public void LengthPrefixOutOfRange ()
        {
            var harness = Harness ();
            harness.SendBytes (new byte[] { 0xff, 0xff, 0xff, 0xff, 0x07, 0x00 });
            harness.Server.Update ();
            CheckRefused (harness, Status.MalformedMessage,
                "Failed to parse the connection request: Message length prefix is out of range");
        }

        [Test]
        public void LengthPrefixAbove32Bits ()
        {
            var harness = Harness ();
            harness.SendBytes (new byte[] { 0x80, 0x80, 0x80, 0x80, 0x10 });
            harness.Server.Update ();
            CheckRefused (harness, Status.MalformedMessage,
                "Failed to parse the connection request: Message length prefix is out of range");
        }

        [Test]
        public void FirstMessageIsNotAConnectionRequest ()
        {
            var harness = Harness ();
            harness.Send (MessageHarness.Request (1));
            harness.Server.Update ();
            CheckRefused (harness, Status.MalformedMessage, "Expected a connection request");
        }

        [Test]
        public void FirstMessageHasNoKnownCase ()
        {
            var harness = Harness ();
            harness.Send (new SchemaMessage ());
            harness.Server.Update ();
            CheckRefused (harness, Status.MalformedMessage, "Expected a connection request");
        }

        [Test]
        public void FirstMessageIsADisconnect ()
        {
            var harness = Harness ();
            harness.Send (MessageHarness.Disconnect (Reason.Closed));
            harness.Server.Update ();
            Assert.AreEqual (0, harness.Receive ().Count);
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void HandshakeTimeout ()
        {
            var harness = Harness ();
            harness.SendBytes (new byte[] { 0x10, 0x00 });
            harness.Server.Update ();
            Assert.AreEqual (0, harness.Receive ().Count);
            Thread.Sleep (3100);
            harness.Server.Update ();
            CheckRefused (harness, Status.Timeout);
        }

        [Test]
        public void Denied ()
        {
            var harness = Harness ();
            harness.Approve = e => e.Request.Deny ();
            harness.Send (MessageHarness.ConnectionRequest (2));
            harness.Server.Update ();
            CheckRefused (harness, Status.Denied);
        }

        [Test]
        public void ApprovalPending ()
        {
            var harness = Harness ();
            var allow = false;
            harness.Approve = e => {
                if (allow)
                    e.Request.Allow ();
            };
            harness.Send (MessageHarness.ConnectionRequest (2));
            harness.Server.Update ();
            harness.Server.Update ();
            Assert.AreEqual (0, harness.Receive ().Count);
            Assert.AreEqual (0, harness.Server.Clients.Count ());
            allow = true;
            harness.Server.Update ();
            Assert.AreEqual (Status.Ok, harness.ReceiveOne ().ConnectionResponse.Status);
            Assert.AreEqual (1, harness.Server.Clients.Count ());
        }

        [Test]
        public void ByteClientDisconnectsWhilePending ()
        {
            var harness = Harness ();
            var requests = new List<ClientRequestingConnectionEventArgs<Request,Response>> ();
            harness.Approve = requests.Add;
            harness.Send (MessageHarness.ConnectionRequest (2));
            harness.Server.Update ();
            Assert.AreEqual (1, requests.Count);
            var client = requests [0].Client;
            Assert.IsTrue (client.Connected);
            harness.DisconnectByteClient ();
            harness.Server.Update ();
            Assert.IsFalse (client.Connected);
            Assert.AreEqual (1, requests.Count);
            Assert.AreEqual (0, harness.Disconnected.Count);
        }

        [Test]
        public void ByteClientClosesWhilePendingWithUnreadBytes ()
        {
            var harness = Harness ();
            var requests = new List<ClientRequestingConnectionEventArgs<Request,Response>> ();
            harness.Approve = requests.Add;
            harness.Send (MessageHarness.ConnectionRequest (2));
            harness.Server.Update ();
            var client = requests [0].Client;
            harness.Send (MessageHarness.Disconnect (Reason.Closed));
            harness.Client.PeerClosed = true;
            harness.Server.Update ();
            Assert.IsFalse (client.Connected);
        }

        [Test]
        public void ByteClientClosesDuringSessionWithUnreadBytes ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Send (MessageHarness.Disconnect (Reason.Closed));
            harness.Client.PeerClosed = true;
            // The core does not poll a client whose call is yielding
            harness.Server.Update ();
            Assert.IsFalse (client.Connected);
        }

        [Test]
        public void StalledMessageEndsTheConnection ()
        {
            var harness = Harness ();
            harness.Server.StallTimeout = 0.1;
            var client = harness.Handshake ();
            // A length prefix whose message never arrives
            harness.SendBytes (new byte[] { 0x10, 0x00 });
            Assert.IsFalse (client.Stream.DataAvailable);
            Thread.Sleep (200);
            Assert.IsFalse (client.Stream.DataAvailable);
            Assert.AreEqual (Reason.Timeout, harness.ReceiveOne ().Disconnect.Reason);
            Assert.IsTrue (harness.Stream.HalfClosed);
        }

        [Test]
        public void ClosingConnectionIsDrainedThenClosed ()
        {
            var harness = Harness ();
            harness.Send (MessageHarness.ConnectionRequest (1));
            harness.Server.Update ();
            CheckRefused (harness, Status.WrongProtocolVersion);
            Assert.IsFalse (harness.Stream.Closed);
            harness.SendBytes (new byte[] { 1, 2, 3 });
            harness.Server.Update ();
            Assert.IsFalse (harness.Stream.Closed);
            Assert.IsFalse (harness.Stream.DataAvailable);
            harness.Client.PeerClosed = true;
            harness.Server.Update ();
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void ClosingConnectionClosesAfterOneSecond ()
        {
            var harness = Harness ();
            harness.Send (MessageHarness.ConnectionRequest (1));
            harness.Server.Update ();
            harness.Server.Update ();
            Assert.IsFalse (harness.Stream.Closed);
            Thread.Sleep (1100);
            harness.Server.Update ();
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void ReadRequests ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            Assert.IsFalse (client.Stream.DataAvailable);
            harness.Send (MessageHarness.Request (1));
            harness.Send (MessageHarness.Request (2, "TestService", "ProcedureSingleArgNoReturn"));
            Assert.IsTrue (client.Stream.DataAvailable);
            var request = client.Stream.Read ();
            Assert.AreEqual (1, request.Id);
            Assert.AreEqual ("ProcedureNoArgsNoReturn", request.Calls [0].Procedure);
            Assert.IsTrue (client.Stream.DataAvailable);
            request = client.Stream.Read ();
            Assert.AreEqual (2, request.Id);
            Assert.AreEqual ("ProcedureSingleArgNoReturn", request.Calls [0].Procedure);
            Assert.IsFalse (client.Stream.DataAvailable);
        }

        [Test]
        public void WriteResponse ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            var response = new Response { Id = 42 };
            response.Error = new Error ("SomeErrorMessage", "StackTrace");
            client.Stream.Write (response);
            var message = harness.ReceiveOne ();
            Assert.AreEqual (ContentCase.Response, message.ContentCase);
            Assert.AreEqual (42, message.Response.Id);
            Assert.AreEqual ("SomeErrorMessage", message.Response.Error.Description);
        }

        [Test]
        public void WriteStreamUpdate ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            var update = new StreamUpdate ();
            update.Results.Add (new StreamResult { Id = 3, Result = new ProcedureResult { Value = 42, Spec = KRPC.Service.TypeSpec.Create (typeof (int)) } });
            ((IMessageStream)client.Stream).Write (update);
            var message = harness.ReceiveOne ();
            Assert.AreEqual (ContentCase.StreamUpdate, message.ContentCase);
            Assert.AreEqual (1, message.StreamUpdate.Results.Count);
            Assert.AreEqual (3, message.StreamUpdate.Results [0].Id);
        }

        [Test]
        public void UnknownCaseIsSkipped ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            // A message holding field 99, a case this server does not know
            harness.SendBytes (new byte[] { 0x04, 0x9a, 0x06, 0x01, 0x00 });
            harness.Send (MessageHarness.Request (1));
            Assert.IsTrue (client.Stream.DataAvailable);
            Assert.AreEqual (1, client.Stream.Read ().Id);
            Assert.AreEqual (0, harness.Receive ().Count);
            Assert.IsTrue (client.Connected);
        }

        [Test]
        public void RequestThatCannotBeDecoded ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            // Names an object identifier the server does not have
            var undecodable = MessageHarness.Request (5, "TestService", "DeleteTestObject");
            undecodable.Request.Calls [0].Arguments.Add (new Schema.KRPC.Argument {
                Position = 0,
                Value = ByteString.CopyFrom (new byte [] { 42 })
            });
            harness.Send (undecodable);
            harness.Send (MessageHarness.Request (6));
            var exn = Assert.Throws<KRPC.Server.Message.RequestDecodeException> (() => {
                var available = client.Stream.DataAvailable;
            });
            Assert.AreEqual (5, exn.Id);
            Assert.IsInstanceOf<ArgumentException> (exn.InnerException);
            Assert.IsTrue (client.Stream.DataAvailable);
            Assert.AreEqual (6, client.Stream.Read ().Id);
            Assert.IsTrue (client.Connected);
        }

        [Test]
        public void ClientDisconnects ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Send (MessageHarness.Disconnect (Reason.Closed));
            Assert.IsFalse (client.Stream.DataAvailable);
            Assert.IsFalse (client.Connected);
            Assert.IsTrue (harness.Stream.HalfClosed);
            Assert.IsFalse (harness.Stream.Closed);
            Assert.AreEqual (0, harness.Disconnected.Count);
            harness.Server.Update ();
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            Assert.AreEqual (0, harness.Server.Clients.Count ());
            Assert.IsFalse (harness.Stream.Closed);
            harness.Client.PeerClosed = true;
            harness.Server.Update ();
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void DisconnectBufferedWhenTheByteClientDisconnects ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Send (MessageHarness.Request (1));
            harness.Send (MessageHarness.Disconnect (Reason.Closed));
            Assert.AreEqual (1, client.Stream.Read ().Id);
            var writer = Logger.Writer;
            var messages = new List<Tuple<string, Logger.Severity>> ();
            try {
                Logger.Writer = (message, severity) => messages.Add (Tuple.Create (message, severity));
                harness.DisconnectByteClient ();
            } finally {
                Logger.Writer = writer;
            }
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            Assert.AreEqual (1, messages.Count);
            StringAssert.Contains ("disconnected (Closed)", messages [0].Item1);
            Assert.AreEqual (Logger.Severity.Info, messages [0].Item2);
        }

        [Test]
        public void FailedWriteEndsTheConnection ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Stream.FailWrites = true;
            client.Stream.Write (new Response { Id = 1 });
            Assert.IsFalse (client.Connected);
            Assert.IsTrue (harness.Stream.Closed);
            harness.Server.Update ();
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
        }

        [Test]
        public void FailedStreamUpdateWriteEndsTheConnection ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Stream.FailWrites = true;
            ((IMessageStream)client.Stream).Write (new StreamUpdate ());
            Assert.IsFalse (client.Connected);
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void ByteClientDisconnects ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.DisconnectByteClient ();
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            Assert.AreEqual (0, harness.Server.Clients.Count ());
        }

        [Test]
        public void Reconnect ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Send (MessageHarness.ConnectionRequest (2, "Bill"));
            Assert.IsFalse (client.Stream.DataAvailable);
            Assert.IsFalse (client.Connected);
            harness.Server.Update ();
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            Assert.AreEqual (2, harness.Connected.Count);
            var newClient = harness.Connected [1];
            Assert.AreEqual ("Bill", newClient.Name);
            Assert.AreNotEqual (client.Guid, newClient.Guid);
            var response = harness.ReceiveOne ().ConnectionResponse;
            Assert.AreEqual (Status.Ok, response.Status);
            CollectionAssert.AreEqual (newClient.Guid.ToByteArray (), response.ClientIdentifier.ToByteArray ());
            CollectionAssert.AreEqual (new [] { newClient }, harness.Server.Clients.ToArray ());

            // The old session writes nothing more
            client.Stream.Write (new Response { Id = 1 });
            Assert.AreEqual (0, harness.Receive ().Count);
        }

        [Test]
        public void ReconnectIsApproved ()
        {
            var harness = Harness ();
            harness.Handshake ();
            harness.Approve = e => e.Request.Deny ();
            harness.Send (MessageHarness.ConnectionRequest (2));
            Assert.IsFalse (harness.Server.Clients.First ().Stream.DataAvailable);
            harness.Server.Update ();
            Assert.AreEqual (Status.Denied, harness.ReceiveOne ().ConnectionResponse.Status);
            Assert.AreEqual (0, harness.Server.Clients.Count ());
            Assert.AreEqual (1, harness.Disconnected.Count);
            Assert.IsTrue (harness.Stream.HalfClosed);
        }

        [Test]
        public void ReconnectWithAnUnsupportedVersionIsRefused ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Send (MessageHarness.ConnectionRequest (1));
            Assert.IsFalse (client.Stream.DataAvailable);
            harness.Server.Update ();
            Assert.AreEqual (Status.WrongProtocolVersion, harness.ReceiveOne ().ConnectionResponse.Status);
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            Assert.AreEqual (0, harness.Server.Clients.Count ());
            Assert.IsTrue (harness.Stream.HalfClosed);
        }

        [Test]
        public void ReconnectEndsTheOldSessionBeforeTheNewOneStarts ()
        {
            var harness = Harness ();
            var events = new List<string> ();
            harness.Server.OnClientConnected += (s, e) => events.Add ("connected " + e.Client.Name);
            harness.Server.OnClientDisconnected += (s, e) => events.Add ("disconnected " + e.Client.Name);
            var client = harness.Handshake ("Jeb");
            harness.Send (MessageHarness.ConnectionRequest (2, "Bill"));
            Assert.IsFalse (client.Stream.DataAvailable);
            harness.Server.Update ();
            CollectionAssert.AreEqual (new [] { "connected Jeb", "disconnected Jeb", "connected Bill" }, events);
        }

        [Test]
        public void UnexpectedMessageIsAProtocolError ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Send (new SchemaMessage { Response = new Schema.KRPC.Response () });
            Assert.IsFalse (client.Stream.DataAvailable);
            CheckDisconnect (harness, Reason.ProtocolError);
            Assert.IsFalse (client.Connected);
            harness.Server.Update ();
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
        }

        [Test]
        public void MalformedMessageInASession ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.SendBytes (new byte[] { 0x04, 0xff, 0xff, 0xff, 0xff });
            Assert.IsFalse (client.Stream.DataAvailable);
            CheckDisconnect (harness, Reason.MalformedMessage);
            Assert.IsFalse (client.Connected);
        }

        [Test]
        public void DisconnectFromTheServer ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            ((IMessageStream)client.Stream).Disconnect (DisconnectReason.MalformedMessage, "Failed to read a request");
            CheckDisconnect (harness, Reason.MalformedMessage);
            harness.Server.Update ();
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
        }

        [Test]
        public void RemoveClient ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            client.Close ();
            CheckDisconnect (harness, Reason.Removed);
            harness.Server.Update ();
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
        }

        [Test]
        public void StopServer ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            var timer = Stopwatch.StartNew ();
            harness.Server.Stop ();
            Assert.GreaterOrEqual (timer.Elapsed.TotalSeconds, 1);
            CheckDisconnect (harness, Reason.ServerStopped);
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void StopServerBeforeTheHandshake ()
        {
            var harness = Harness ();
            harness.Server.Update ();
            harness.Server.Stop ();
            CheckDisconnect (harness, Reason.ServerStopped);
            Assert.AreEqual (0, harness.Connected.Count);
            Assert.AreEqual (0, harness.Disconnected.Count);
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void StopServerWhileApprovalIsPending ()
        {
            var harness = Harness ();
            harness.Approve = e => { };
            harness.Send (MessageHarness.ConnectionRequest (2));
            harness.Server.Update ();
            Assert.AreEqual (0, harness.Receive ().Count);
            harness.Server.Stop ();
            CheckDisconnect (harness, Reason.ServerStopped);
            Assert.AreEqual (0, harness.Connected.Count);
            Assert.AreEqual (0, harness.Disconnected.Count);
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void StopServerDrainsUntilThePeerCloses ()
        {
            var harness = Harness ();
            harness.Handshake ();
            var peer = new Thread (() => {
                Thread.Sleep (100);
                harness.Client.PeerClosed = true;
            });
            peer.Start ();
            var timer = Stopwatch.StartNew ();
            harness.Server.Stop ();
            peer.Join ();
            Assert.Less (timer.Elapsed.TotalSeconds, 0.9);
            CheckDisconnect (harness, Reason.ServerStopped);
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void PartialMessageWaitsForTheRest ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.SendBytes (new byte[] { 0x10, 0x00 });
            Assert.IsFalse (client.Stream.DataAvailable);
            Assert.IsTrue (client.Connected);
            Assert.AreEqual (0, harness.Receive ().Count);
        }

        [Test]
        public void ClientLabel ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            var bytes = client.Guid.ToByteArray ();
            var expected = "TestClientAddress (" + string.Concat (bytes.Take (4).Select (x => x.ToString ("x2", System.Globalization.CultureInfo.InvariantCulture))) + ")";
            Assert.AreEqual (expected, client.Label ());
        }
    }
}
