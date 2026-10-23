using System.Linq;
using System.Text;
using System.Threading;
using KRPC.Server.WebSockets;
using NUnit.Framework;
using ContentCase = KRPC.Schema.KRPC.Message.ContentOneofCase;
using Reason = KRPC.Schema.KRPC.Disconnect.Types.Reason;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;

namespace KRPC.Test.Server.WebSockets
{
    [TestFixture]
    public class ServerTest
    {
        const string validRequest =
            "GET / HTTP/1.1\r\n" +
            "Host: localhost\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
            "Sec-WebSocket-Version: 13\r\n\r\n";

        const string validResponse =
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n";

        static MessageHarness Harness ()
        {
            return new MessageHarness (byteServer => new KRPC.Server.WebSockets.Server (byteServer), true);
        }

        static void CheckValidConnectionRequest (string request)
        {
            var harness = Harness ();
            harness.SendBytes (Encoding.ASCII.GetBytes (request));
            harness.Server.Update ();
            Assert.AreEqual (validResponse, Encoding.ASCII.GetString (harness.ReceiveBytes ()));
            Assert.AreEqual (0, harness.Server.Clients.Count ());
            harness.Handshake ();
            Assert.AreEqual (1, harness.Server.Clients.Count ());
        }

        [Test]
        public void ValidConnectionRequest ()
        {
            CheckValidConnectionRequest (validRequest);
        }

        [Test]
        public void SubprotocolIsIgnored ()
        {
            CheckValidConnectionRequest (
                "GET / HTTP/1.1\r\n" +
                "Host: localhost\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                "Sec-WebSocket-Version: 13\r\n" +
                "Sec-WebSocket-Protocol: chat, krpc.1\r\n\r\n");
        }

        [Test]
        public void QueryStringIsIgnored ()
        {
            CheckValidConnectionRequest (
                "GET /?name=Jebediah%20Kerman HTTP/1.1\r\n" +
                "Host: localhost\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                "Sec-WebSocket-Version: 13\r\n\r\n");
        }

        [Test]
        public void ValidConnectionRequestWithKeepAlive ()
        {
            CheckValidConnectionRequest (
                "GET / HTTP/1.1\r\n" +
                "Host: localhost\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: keep-alive, Upgrade\r\n" +
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                "Sec-WebSocket-Version: 13\r\n\r\n");
        }

        [Test]
        public void ValidConnectionRequestCaseInsensitive ()
        {
            CheckValidConnectionRequest (
                "get / HTTP/1.1\r\n" +
                "HOST: localhost\r\n" +
                "Upgrade: WebSocket\r\n" +
                "Connection: UPGRADE\r\n" +
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                "Sec-WEBSOCKET-Version: 13\r\n\r\n");
        }

        [Test]
        public void ValidConnectionRequestSplitAcrossReads ()
        {
            var harness = Harness ();
            var requestBytes = Encoding.ASCII.GetBytes (validRequest);
            var split = requestBytes.Length / 2;
            harness.SendBytes (requestBytes.Take (split).ToArray ());
            harness.Server.Update ();
            Assert.AreEqual (0, harness.ReceiveBytes ().Length);
            harness.SendBytes (requestBytes.Skip (split).ToArray ());
            harness.Server.Update ();
            Assert.AreEqual (validResponse, Encoding.ASCII.GetString (harness.ReceiveBytes ()));
        }

        static string CheckInvalidConnectionRequest (string request)
        {
            return CheckInvalidConnectionRequest (Encoding.ASCII.GetBytes (request));
        }

        static string CheckInvalidConnectionRequest (byte[] request)
        {
            var harness = Harness ();
            harness.SendBytes (request);
            harness.Server.Update ();
            Assert.AreEqual (0, harness.Server.Clients.Count ());
            Assert.IsTrue (harness.Stream.HalfClosed);
            return Encoding.ASCII.GetString (harness.ReceiveBytes ());
        }

        [Test]
        public void HandshakeInABinaryMessage ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            var client = harness.Handshake ("Jebediah Kerman");
            Assert.AreEqual ("Jebediah Kerman", client.Name);
        }

        [Test]
        public void UnsupportedProtocolVersion ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            harness.Send (MessageHarness.ConnectionRequest (1));
            harness.Server.Update ();
            Assert.AreEqual (Status.WrongProtocolVersion, harness.ReceiveOne ().ConnectionResponse.Status);
            Assert.IsTrue (harness.ReceivedClose);
            Assert.AreEqual (0, harness.Server.Clients.Count ());
        }

        [Test]
        public void ReadRequestAndWriteResponse ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            var client = harness.Handshake ();
            harness.Send (MessageHarness.Request (3));
            Assert.IsTrue (client.Stream.DataAvailable);
            Assert.AreEqual (3, client.Stream.Read ().Id);
            client.Stream.Write (new KRPC.Service.Messages.Response { Id = 3 });
            var message = harness.ReceiveOne ();
            Assert.AreEqual (ContentCase.Response, message.ContentCase);
            Assert.AreEqual (3, message.Response.Id);
        }

        [Test]
        public void RequestInFragmentsInterleavedWithAPing ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            var client = harness.Handshake ();
            var payload = Google.Protobuf.MessageExtensions.ToByteArray (MessageHarness.Request (4));
            var maskingKey = new byte[] { 1, 2, 3, 4 };
            var frame1 = new Frame (OpCode.Binary, payload.Take (3).ToArray ());
            frame1.Header.FinalFragment = false;
            frame1.Header.MaskingKey = maskingKey;
            var ping = new Frame (OpCode.Ping, new byte[] { 9 });
            ping.Header.MaskingKey = maskingKey;
            var frame2 = new Frame (OpCode.Continue, payload.Skip (3).ToArray ());
            frame2.Header.MaskingKey = maskingKey;
            harness.SendBytes (frame1.ToBytes ());
            harness.SendBytes (ping.ToBytes ());
            harness.SendBytes (frame2.ToBytes ());
            Assert.IsTrue (client.Stream.DataAvailable);
            Assert.AreEqual (4, client.Stream.Read ().Id);
            var pong = harness.ReceiveBytes ();
            Assert.AreEqual (Frame.Pong (new byte[] { 9 }).ToBytes (), pong);

            // A second message after the fragmented one
            harness.Send (MessageHarness.Request (5));
            Assert.IsTrue (client.Stream.DataAvailable);
            Assert.AreEqual (5, client.Stream.Read ().Id);
        }

        [Test]
        public void StalledFragmentedMessageEndsTheConnection ()
        {
            var harness = Harness ();
            harness.Server.StallTimeout = 0.1;
            harness.Upgrade ();
            var client = harness.Handshake ();
            // The first fragment of a message whose last fragment never arrives
            var payload = Google.Protobuf.MessageExtensions.ToByteArray (MessageHarness.Request (4));
            var frame = new Frame (OpCode.Binary, payload.Take (3).ToArray ());
            frame.Header.FinalFragment = false;
            frame.Header.MaskingKey = new byte[] { 1, 2, 3, 4 };
            harness.SendBytes (frame.ToBytes ());
            Assert.IsFalse (client.Stream.DataAvailable);
            Thread.Sleep (200);
            Assert.IsFalse (client.Stream.DataAvailable);
            Assert.AreEqual (Reason.Timeout, harness.ReceiveOne ().Disconnect.Reason);
            Assert.IsTrue (harness.ReceivedClose);
        }

        [Test]
        public void UnmaskedFrameEndsTheConnection ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            var client = harness.Handshake ();
            harness.SendBytes (new Frame (OpCode.Binary, new byte[] { 1, 2 }).ToBytes ());
            Assert.IsFalse (client.Stream.DataAvailable);
            Assert.AreEqual (Frame.Close (1002, "Payload is not masked").ToBytes (), harness.ReceiveBytes ());
            Assert.IsFalse (harness.Stream.Closed);
            var close = Frame.Close (1002);
            close.Header.MaskingKey = new byte[] { 1, 2, 3, 4 };
            harness.SendBytes (close.ToBytes ());
            harness.Server.Update ();
            Assert.IsTrue (harness.Stream.Closed);
            Assert.AreEqual (0, harness.ReceiveBytes ().Length);
        }

        [Test]
        public void InvalidFrameBeforeTheHandshakeWritesOneCloseFrame ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            harness.SendBytes (new Frame (OpCode.Binary, new byte[] { 1, 2 }).ToBytes ());
            harness.Server.Update ();
            Assert.AreEqual (Frame.Close (1002, "Payload is not masked").ToBytes (), harness.ReceiveBytes ());
            Assert.AreEqual (0, harness.Server.Clients.Count ());
        }

        [Test]
        public void FrameLengthOutOfRange ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            var client = harness.Handshake ();
            harness.SendBytes (new byte[] { 0x82, 0xff, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 1, 2, 3, 4 });
            Assert.IsFalse (client.Stream.DataAvailable);
            Assert.AreEqual (Frame.Close (1009, "Frame is too large").ToBytes (), harness.ReceiveBytes ());
            Assert.IsFalse (harness.Stream.Closed);
            harness.Client.PeerClosed = true;
            harness.Server.Update ();
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void StopBeforeTheUpgradeWritesNoFrame ()
        {
            var harness = Harness ();
            harness.Client.PeerClosed = true;
            harness.Server.Stop ();
            Assert.AreEqual (0, harness.ReceiveBytes ().Length);
            Assert.IsTrue (harness.Stream.HalfClosed);
        }

        [Test]
        public void StopWritesADisconnectThenOneCloseFrame ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            harness.Handshake ();
            harness.Client.PeerClosed = true;
            harness.Server.Stop ();
            var disconnect = new KRPC.Schema.KRPC.Message { Disconnect = new KRPC.Schema.KRPC.Disconnect {
                    Reason = Reason.ServerStopped,
                    Message = "The server stopped"
                } };
            var expected = Frame.Binary (Google.Protobuf.MessageExtensions.ToByteArray (disconnect)).ToBytes ()
                .Concat (Frame.Close (1000).ToBytes ()).ToArray ();
            Assert.AreEqual (expected, harness.ReceiveBytes ());
        }

        [Test]
        public void DrainEndsAtTheClosingFrameFromTheClient ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            var client = harness.Handshake ();
            ((KRPC.Server.IMessageStream)client.Stream).Disconnect (KRPC.Server.DisconnectReason.Timeout, "Timed out");
            Assert.AreEqual (Reason.Timeout, harness.ReceiveOne ().Disconnect.Reason);
            Assert.IsTrue (harness.ReceivedClose);
            var maskingKey = new byte[] { 1, 2, 3, 4 };
            harness.SendBytes (Frame.Binary (new byte[] { 1, 2, 3 }, maskingKey).ToBytes ());
            harness.Server.Update ();
            Assert.IsFalse (harness.Stream.Closed);
            var close = Frame.Close (1000);
            close.Header.MaskingKey = maskingKey;
            harness.SendBytes (close.ToBytes ());
            harness.Server.Update ();
            Assert.IsTrue (harness.Stream.Closed);
            Assert.AreEqual (0, harness.ReceiveBytes ().Length);
        }

        [Test]
        public void MalformedMessageInASession ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            var client = harness.Handshake ();
            harness.SendBytes (Frame.Binary (new byte[] { 0xff, 0xff }, new byte[] { 1, 2, 3, 4 }).ToBytes ());
            Assert.IsFalse (client.Stream.DataAvailable);
            var messages = harness.Receive ();
            Assert.AreEqual (Reason.MalformedMessage, messages [0].Disconnect.Reason);
            Assert.IsTrue (harness.ReceivedClose);
        }

        [Test]
        public void DisconnectIsAnsweredWithACloseFrame ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            var client = harness.Handshake ();
            harness.Send (MessageHarness.Disconnect (Reason.Closed));
            Assert.IsFalse (client.Stream.DataAvailable);
            Assert.AreEqual (0, harness.Receive ().Count);
            Assert.IsTrue (harness.ReceivedClose);
            Assert.IsFalse (harness.Stream.Closed);
            var close = Frame.Close (1000);
            close.Header.MaskingKey = new byte[] { 1, 2, 3, 4 };
            harness.SendBytes (close.ToBytes ());
            harness.Server.Update ();
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void CloseFrameFromTheClientIsAnsweredThenClosed ()
        {
            var harness = Harness ();
            harness.Upgrade ();
            var client = harness.Handshake ();
            var close = Frame.Close (1000);
            close.Header.MaskingKey = new byte[] { 1, 2, 3, 4 };
            harness.SendBytes (close.ToBytes ());
            Assert.IsFalse (client.Stream.DataAvailable);
            Assert.AreEqual (Frame.Close (1000).ToBytes (), harness.ReceiveBytes ());
            Assert.IsFalse (harness.Stream.Closed);
            harness.Server.Update ();
            Assert.IsTrue (harness.Stream.Closed);
        }

        [Test]
        public void InvalidConnectionRequestGarbage ()
        {
            var response = CheckInvalidConnectionRequest ("deadbeef\r\n\r\n");
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\n", response);
        }

        [Test]
        public void InvalidConnectionRequestDuplicateFields ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 13\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\n", response);
        }

        [Test]
        public void InvalidConnectionRequestWrongProtocolVersion ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/2\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 13\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 505 HTTP Version Not Supported\r\n\r\n", response);
        }

        [Test]
        public void InvalidConnectionRequestWrongURI ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET /foo HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 13\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 404 Not Found\r\n\r\n", response);
        }

        [Test]
        public void InvalidConnectionRequestWrongMethod ()
        {
            var response = CheckInvalidConnectionRequest (
                               "POST / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 13\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 405 Method Not Allowed\r\n\r\nExpected a GET request.", response);
        }

        [Test]
        public void InvalidConnectionRequestHostFieldMissing ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nHost field not set.", response);
        }

        [Test]
        public void InvalidConnectionRequestUpgradeFieldMissing ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nUpgrade field not set to websocket.", response);
        }

        [Test]
        public void InvalidConnectionRequestUpgradeFieldMalformed ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: foo\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nUpgrade field not set to websocket.", response);
        }

        [Test]
        public void InvalidConnectionRequestConnectionFieldMissing ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nConnection field not set to Upgrade.", response);
        }

        [Test]
        public void InvalidConnectionRequestConnectionFieldMalformed ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: foo\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nConnection field not set to Upgrade.", response);
        }

        [Test]
        public void InvalidConnectionRequestWebSocketKeyMissing ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nSec-WebSocket-Key field not set.", response);
        }

        [Test]
        public void InvalidConnectionRequestWebSocketKeyTooShort ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: Zm9v\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nFailed to decode Sec-WebSocket-Key\nExpected 16 bytes, got 3 bytes.", response);
        }

        [Test]
        public void InvalidConnectionRequestWebSocketKeyMalformed ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: foo\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nFailed to decode Sec-WebSocket-Key\nNot a valid base64 string.", response);
        }

        [Test]
        public void InvalidConnectionRequestWebSocketKeyRepeated ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==, dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 13\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nFailed to decode Sec-WebSocket-Key\nExpected 16 bytes, got 0 bytes.", response);
        }

        [Test]
        public void InvalidConnectionRequestWebSocketVersionMissing ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 426 Upgrade Required\r\nSec-WebSocket-Version: 13\r\n\r\n", response);
        }

        [Test]
        public void InvalidConnectionRequestWrongWebSocketVersion ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 426 Upgrade Required\r\nSec-WebSocket-Version: 13\r\n\r\n", response);
        }
    }
}
