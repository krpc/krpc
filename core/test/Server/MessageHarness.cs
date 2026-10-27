using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using KRPC.Server;
using KRPC.Server.WebSockets;
using KRPC.Service.Messages;
using Moq;
using NUnit.Framework;
using ContentCase = KRPC.Schema.KRPC.Message.ContentOneofCase;
using SchemaMessage = KRPC.Schema.KRPC.Message;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;

namespace KRPC.Test.Server
{
    /// <summary>
    /// Drives a message server over one in-memory byte client, sending it messages and
    /// reading back the messages it writes.
    /// </summary>
    sealed class MessageHarness
    {
        readonly bool webSockets;
        readonly MemoryStream input = new MemoryStream ();
        readonly MemoryStream output = new MemoryStream ();
        int outputRead;

        public MessageHarness (Func<IServer<byte,byte>, KRPC.Server.Message.Server> create, bool useWebSockets = false)
        {
            webSockets = useWebSockets;
            ByteServer = new Mock<IServer<byte,byte>> ();
            Stream = new TestStream (input, output);
            Client = new TestClient (Stream);
            Server = create (ByteServer.Object);
            Approve = e => e.Request.Allow ();
            Server.OnClientRequestingConnection += (s, e) => Approve (e);
            Server.OnClientConnected += (s, e) => Connected.Add (e.Client);
            Server.OnClientDisconnected += (s, e) => Disconnected.Add (e.Client);
            Server.Start ();
            var args = new ClientRequestingConnectionEventArgs<byte,byte> (Client);
            ByteServer.Raise (m => m.OnClientRequestingConnection += null, args);
            Assert.IsTrue (args.Request.ShouldAllow);
            ByteServer.Raise (m => m.OnClientConnected += null, new ClientConnectedEventArgs<byte,byte> (Client));
        }

        public Mock<IServer<byte,byte>> ByteServer { get; private set; }

        public TestStream Stream { get; private set; }

        public TestClient Client { get; private set; }

        public KRPC.Server.Message.Server Server { get; private set; }

        /// <summary>
        /// How the server's handler answers a connection request.
        /// </summary>
        public Action<ClientRequestingConnectionEventArgs<Request,Response>> Approve { get; set; }

        public List<IClient<Request,Response>> Connected { get; } = new List<IClient<Request,Response>> ();

        public List<IClient<Request,Response>> Disconnected { get; } = new List<IClient<Request,Response>> ();

        /// <summary>
        /// Whether the server wrote a websockets close frame.
        /// </summary>
        public bool ReceivedClose { get; private set; }

        public void SendBytes (byte[] data)
        {
            var position = input.Position;
            input.Seek (0, SeekOrigin.End);
            input.Write (data, 0, data.Length);
            input.Position = position;
        }

        public void Send (SchemaMessage message)
        {
            if (webSockets) {
                SendBytes (Frame.Binary (message.ToByteArray (), new byte[] { 0xde, 0xad, 0xbe, 0xef }).ToBytes ());
            } else {
                using (var stream = new MemoryStream ()) {
                    message.WriteDelimitedTo (stream);
                    SendBytes (stream.ToArray ());
                }
            }
        }

        /// <summary>
        /// The bytes the server has written since the last call.
        /// </summary>
        public byte[] ReceiveBytes ()
        {
            var data = output.ToArray ();
            var result = new byte [data.Length - outputRead];
            Array.Copy (data, outputRead, result, 0, result.Length);
            outputRead = data.Length;
            return result;
        }

        /// <summary>
        /// The messages the server has written since the last call.
        /// </summary>
        public IList<SchemaMessage> Receive ()
        {
            var data = ReceiveBytes ();
            var messages = new List<SchemaMessage> ();
            var offset = 0;
            while (offset < data.Length) {
                if (webSockets) {
                    // Frames from the server are unmasked
                    var opCode = (OpCode)(data [offset] & 0x0f);
                    var length = data [offset + 1] & 0x7f;
                    var headerLength = 2;
                    if (length == 126) {
                        length = (data [offset + 2] << 8) | data [offset + 3];
                        headerLength = 4;
                    }
                    var payload = new byte [length];
                    Array.Copy (data, offset + headerLength, payload, 0, length);
                    offset += headerLength + length;
                    if (opCode == OpCode.Close)
                        ReceivedClose = true;
                    else
                        messages.Add (SchemaMessage.Parser.ParseFrom (payload));
                } else {
                    var stream = new CodedInputStream (data, offset, data.Length - offset);
                    var message = new SchemaMessage ();
                    stream.ReadMessage (message);
                    offset += (int)stream.Position;
                    messages.Add (message);
                }
            }
            return messages;
        }

        /// <summary>
        /// The one message the server has written since the last call.
        /// </summary>
        public SchemaMessage ReceiveOne ()
        {
            var messages = Receive ();
            Assert.AreEqual (1, messages.Count);
            return messages [0];
        }

        /// <summary>
        /// Upgrade a websockets connection, discarding the server's response.
        /// </summary>
        public void Upgrade ()
        {
            SendBytes (System.Text.Encoding.ASCII.GetBytes (
                "GET / HTTP/1.1\r\n" +
                "Host: localhost\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                "Sec-WebSocket-Version: 13\r\n\r\n"));
            Server.Update ();
            var response = System.Text.Encoding.ASCII.GetString (ReceiveBytes ());
            StringAssert.StartsWith ("HTTP/1.1 101 Switching Protocols\r\n", response);
        }

        /// <summary>
        /// Complete the handshake, and return the client of the session it starts.
        /// </summary>
        public IClient<Request,Response> Handshake (string name = "Test Client")
        {
            Send (ConnectionRequest (2, name));
            Server.Update ();
            var response = ReceiveOne ();
            Assert.AreEqual (ContentCase.ConnectionResponse, response.ContentCase);
            Assert.AreEqual (Status.Ok, response.ConnectionResponse.Status);
            var client = Connected [Connected.Count - 1];
            CollectionAssert.AreEqual (client.Guid.ToByteArray (), response.ConnectionResponse.ClientIdentifier.ToByteArray ());
            return client;
        }

        public void DisconnectByteClient ()
        {
            Client.PeerClosed = true;
            ByteServer.Raise (m => m.OnClientDisconnected += null, new ClientDisconnectedEventArgs<byte,byte> (Client));
        }

        public static SchemaMessage ConnectionRequest (uint protocolVersion, string name = "Test Client")
        {
            var request = new Schema.KRPC.ConnectionRequest ();
            request.ClientName = name;
            request.ProtocolVersion = protocolVersion;
            return new SchemaMessage { ConnectionRequest = request };
        }

        public static SchemaMessage Request (ulong id, string service = "TestService", string procedure = "ProcedureNoArgsNoReturn")
        {
            var request = new Schema.KRPC.Request ();
            request.Id = id;
            request.Calls.Add (new Schema.KRPC.ProcedureCall { Service = service, Procedure = procedure });
            return new SchemaMessage { Request = request };
        }

        public static SchemaMessage Disconnect (Schema.KRPC.Disconnect.Types.Reason reason)
        {
            return new SchemaMessage { Disconnect = new Schema.KRPC.Disconnect { Reason = reason } };
        }
    }
}
