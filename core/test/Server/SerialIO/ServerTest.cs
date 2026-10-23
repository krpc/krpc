using System.Linq;
using System.Threading;
using KRPC.Server.SerialIO;
using NUnit.Framework;
using ContentCase = KRPC.Schema.KRPC.Message.ContentOneofCase;
using Reason = KRPC.Schema.KRPC.Disconnect.Types.Reason;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;

namespace KRPC.Test.Server.SerialIO
{
    [TestFixture]
    public class ServerTest
    {
        static MessageHarness Harness ()
        {
            return new MessageHarness (byteServer => new KRPC.Server.SerialIO.Server (byteServer));
        }

        [Test]
        public void Handshake ()
        {
            var harness = Harness ();
            var client = harness.Handshake ("Jebediah Kerman!!!");
            Assert.AreEqual ("Jebediah Kerman!!!", client.Name);
            Assert.AreEqual (1, harness.Server.Clients.Count ());
        }

        [Test]
        public void NoTimeoutBeforeTheHandshake ()
        {
            var harness = Harness ();
            harness.Server.Update ();
            Thread.Sleep (3100);
            harness.Server.Update ();
            Assert.AreEqual (0, harness.Receive ().Count);
            harness.Handshake ();
        }

        [Test]
        public void RefusalLeavesTheLineOpen ()
        {
            var harness = Harness ();
            harness.Send (MessageHarness.ConnectionRequest (1));
            harness.Server.Update ();
            Assert.AreEqual (Status.WrongProtocolVersion, harness.ReceiveOne ().ConnectionResponse.Status);
            Assert.IsFalse (harness.Stream.HalfClosed);
            Assert.IsFalse (harness.Stream.Closed);
            harness.Handshake ();
        }

        [Test]
        public void DisconnectLeavesTheLineOpen ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Send (MessageHarness.Disconnect (Reason.Closed));
            Assert.IsFalse (client.Stream.DataAvailable);
            harness.Server.Update ();
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            Assert.IsFalse (harness.Stream.Closed);
            var newClient = harness.Handshake ();
            Assert.AreNotEqual (client.Guid, newClient.Guid);
        }

        [Test]
        public void Reconnect ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Send (MessageHarness.ConnectionRequest (2));
            Assert.IsFalse (client.Stream.DataAvailable);
            harness.Server.Update ();
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            Assert.AreEqual (Status.Ok, harness.ReceiveOne ().ConnectionResponse.Status);
            Assert.AreEqual (2, harness.Connected.Count);
        }

        [Test]
        public void RefusedReconnectLeavesTheLineOpen ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Send (MessageHarness.ConnectionRequest (1));
            Assert.IsFalse (client.Stream.DataAvailable);
            harness.Server.Update ();
            Assert.AreEqual (Status.WrongProtocolVersion, harness.ReceiveOne ().ConnectionResponse.Status);
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            Assert.IsFalse (harness.Stream.HalfClosed);
            Assert.IsFalse (harness.Stream.Closed);
            harness.Handshake ();
        }

        [Test]
        public void StopServer ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.Server.Stop ();
            Assert.AreEqual (Reason.ServerStopped, harness.ReceiveOne ().Disconnect.Reason);
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            Assert.IsFalse (harness.Stream.HalfClosed);
        }

        [Test]
        public void MalformedMessageThenReadsTheRestAsANewMessage ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.SendBytes (new byte[] { 0x04, 0xff, 0xff, 0xff, 0xff });
            harness.Send (MessageHarness.ConnectionRequest (2));
            Assert.IsFalse (client.Stream.DataAvailable);
            var disconnect = harness.ReceiveOne ();
            Assert.AreEqual (ContentCase.Disconnect, disconnect.ContentCase);
            Assert.AreEqual (Reason.MalformedMessage, disconnect.Disconnect.Reason);
            harness.Server.Update ();
            Assert.AreEqual (Status.Ok, harness.ReceiveOne ().ConnectionResponse.Status);
            Assert.AreEqual (2, harness.Connected.Count);
        }

        [Test]
        public void OutOfRangeLengthIsDropped ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            harness.SendBytes (new byte[] { 0xff, 0xff, 0xff, 0xff, 0x07, 0x00 });
            Assert.IsFalse (client.Stream.DataAvailable);
            Assert.AreEqual (Reason.MalformedMessage, harness.ReceiveOne ().Disconnect.Reason);
            harness.Server.Update ();
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            harness.Handshake ();
        }

        [Test]
        public void StalledMessageIsDropped ()
        {
            var harness = Harness ();
            var client = harness.Handshake ();
            // A length prefix whose message never arrives
            harness.SendBytes (new byte[] { 0x10, 0x00 });
            Assert.IsFalse (client.Stream.DataAvailable);
            Thread.Sleep (3100);
            Assert.IsFalse (client.Stream.DataAvailable);
            var disconnect = harness.ReceiveOne ();
            Assert.AreEqual (Reason.Timeout, disconnect.Disconnect.Reason);
            harness.Server.Update ();
            CollectionAssert.AreEqual (new [] { client }, harness.Disconnected);
            harness.Handshake ();
        }

        [Test]
        public void StalledConnectionRequestIsDropped ()
        {
            var harness = Harness ();
            harness.SendBytes (new byte[] { 0x10, 0x00 });
            harness.Server.Update ();
            Thread.Sleep (3100);
            harness.Server.Update ();
            Assert.AreEqual (Status.Timeout, harness.ReceiveOne ().ConnectionResponse.Status);
            harness.Handshake ();
        }
    }
}
