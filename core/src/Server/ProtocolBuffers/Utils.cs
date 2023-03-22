using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Google.Protobuf;
using KRPC.Utils;
using ConnectionResponse = KRPC.Schema.KRPC.ConnectionResponse;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;

namespace KRPC.Server.ProtocolBuffers
{
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    static class Utils
    {

        static IDictionary<IClient<byte, byte>, Stopwatch> readMessageTimers = new Dictionary<IClient<byte, byte>, Stopwatch> ();
        static IDictionary<IClient<byte, byte>, DynamicBuffer> readMessageBuffers = new Dictionary<IClient<byte, byte>, DynamicBuffer> ();

        /// <summary>
        /// Read a message from the client. If a partial message is received, its data is saved
        /// and will be resumed on the next call. Timeout is set to true if the receipt times out.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidMethodWithUnusedGenericTypeRule")]
        public static T ReadMessage<T> (IClient<byte, byte> client, out bool timeout) where T : class, IMessage<T>, new()
        {
            timeout = false;
            DynamicBuffer buffer = null;
            readMessageBuffers.TryGetValue(client, out buffer);
            var request = ReadMessage<T> (client.Stream, ref buffer);
            if (request == null) {
                if (readMessageTimers.ContainsKey (client) && readMessageTimers [client].ElapsedSeconds () > 3) {
                    readMessageTimers.Remove (client);
                    readMessageBuffers.Remove (client);
                    timeout = true;
                    return null;
                }
                readMessageBuffers [client] = buffer;
                if (!readMessageTimers.ContainsKey (client)) {
                    var timer = new Stopwatch ();
                    timer.Start ();
                    readMessageTimers [client] = timer;
                }
                return null;
            }
            readMessageBuffers.Remove (client);
            return request;
        }

        /// <summary>
        /// Read a message from the client. If a partial message is received, its data is saved
        /// in the DynamicBuffer so that this method can be called to try again later.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidMethodWithUnusedGenericTypeRule")]
        public static T ReadMessage<T> (IStream<byte,byte> stream, ref DynamicBuffer data) where T : class, IMessage<T>, new()
        {
            if (!stream.DataAvailable)
                return null;

            if (data == null)
                data = new DynamicBuffer ();
            byte[] buffer = new byte[4096]; //TODO: sensible default???

            int read = stream.Read (buffer, 0, buffer.Length);
            if (read == 0)
                return null;
            data.Append (buffer, 0, read);

            var codedStream = new CodedInputStream (data.GetBuffer (), 0, data.Length);
            // Get the protobuf message size
            int size;
            try {
                size = (int)codedStream.ReadUInt32 ();
            } catch (InvalidProtocolBufferException) {
                return null;
            }
            int totalSize = (int)codedStream.Position + size;
            // Check if enough data is available, if not then delay the decoding
            if (data.Length < totalSize)
                return null;
            // Decode the request
            var message = new T ();
            message.MergeFrom (codedStream);
            return message;
        }

        /// <summary>
        /// Attempt to parse a message from a byte buffer. Returns the number of bytes consumed.
        /// If there is not enough data to decode a message, returns zero.
        /// </summary>
        public static int ReadMessage <T> (ref T message, MessageParser<T> parser, byte[] data,
                                           int offset, int length) where T : IMessage<T>, new()
        {
            var codedStream = new CodedInputStream (data, offset, length);
            // Get the protobuf message size
            var size = (int)codedStream.ReadUInt32 ();
            var totalSize = (int)codedStream.Position + size;
            // Check if enough data is available
            if (length < totalSize)
                return 0;
            // Decode the message
            // FIXME: If multiple requests are received, decoding a single request fails unless
            // the coded stream is recreated to be precisely the message size. Why is this?
            codedStream = new CodedInputStream (data, offset + (int)codedStream.Position, size);
            message = parser.ParseFrom (codedStream);
            return totalSize;
        }

        /// <summary>
        /// Write a message
        /// </summary>
        public static void WriteMessage (IStream<byte,byte> stream, IMessage message)
        {
            var codedOutputStream = new CodedOutputStream (new ByteOutputAdapterStream (stream), true);
            codedOutputStream.WriteMessage (message);
            codedOutputStream.Flush ();
        }

        /// <summary>
        /// Write a connection response message
        /// </summary>
        public static void WriteConnectionResponse (IClient<byte, byte> client)
        {
            var response = new ConnectionResponse ();
            response.Status = Status.Ok;
            response.ClientIdentifier = ByteString.CopyFrom (client.Guid.ToByteArray ());
            WriteMessage (client.Stream, response);
        }

        /// <summary>
        /// Write a connection response message
        /// </summary>
        public static void WriteConnectionResponse (
            IClient<byte, byte> client, Status errorStatus, string errorMessage)
        {
            if (errorStatus == Status.Ok)
                throw new ArgumentException ("Error response must have a non-OK status code");
            var response = new ConnectionResponse ();
            response.Status = errorStatus;
            response.Message = errorMessage;
            WriteMessage (client.Stream, response);
        }
    }
}
