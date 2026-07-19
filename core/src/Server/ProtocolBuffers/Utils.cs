using System;
using System.Collections.Generic;
using System.Diagnostics;
using Google.Protobuf;
using KRPC.Utils;
using ConnectionResponse = KRPC.Schema.KRPC.ConnectionResponse;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;

namespace KRPC.Server.ProtocolBuffers
{
    static class Utils
    {
        // Size of the chunk read from the stream on each call. Messages larger than this are
        // accumulated across successive reads via the DynamicBuffer.
        const int ReadChunkSize = 4096;

        static IDictionary<IClient<byte, byte>, Stopwatch> readMessageTimers = new Dictionary<IClient<byte, byte>, Stopwatch> ();
        static IDictionary<IClient<byte, byte>, DynamicBuffer> readMessageBuffers = new Dictionary<IClient<byte, byte>, DynamicBuffer> ();

        /// <summary>
        /// Read a message from the client. If a partial message is received, its data is saved
        /// and will be resumed on the next call. Timeout is set to true if the receipt times out.
        /// </summary>
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
        public static T ReadMessage<T> (IStream<byte,byte> stream, ref DynamicBuffer data) where T : class, IMessage<T>, new()
        {
            if (!stream.DataAvailable)
                return null;

            if (data == null)
                data = new DynamicBuffer ();

            // Read straight into the accumulation buffer, without an intermediate array or copy.
            var backing = data.Reserve (ReadChunkSize);
            int read = stream.Read (backing, data.Length, ReadChunkSize);
            if (read == 0)
                return null;
            data.Length += read;

            // Messages are length-delimited: a varint byte-length prefix followed by the message body.
            int prefixLength;
            int size;
            try {
                size = DecodeMessageLength (data.GetBuffer (), 0, data.Length, out prefixLength);
            } catch (InvalidOperationException) {
                // Malformed length prefix; wait for the receive to time out.
                return null;
            }
            if (prefixLength == 0)
                return null; // The length prefix has not been fully received yet.
            if (data.Length < prefixLength + size)
                return null; // The message body has not been fully received yet.
            // Parse the message body straight out of the buffer, with no copy or intermediate stream,
            // reading exactly the message's own bytes.
            var message = new T ();
            message.MergeFrom (data.GetBuffer (), prefixLength, size);
            return message;
        }

        /// <summary>
        /// Attempt to parse a message from a byte buffer. Returns the number of bytes consumed.
        /// If there is not enough data to decode a message, returns zero.
        /// </summary>
        public static int ReadMessage <T> (ref T message, MessageParser<T> parser, byte[] data,
                                           int offset, int length) where T : IMessage<T>, new()
        {
            // Messages are length-delimited: a varint byte-length prefix followed by the message body.
            int prefixLength;
            var size = DecodeMessageLength (data, offset, length, out prefixLength);
            if (prefixLength == 0)
                return 0; // The length prefix has not been fully received yet.
            var totalSize = prefixLength + size;
            if (length < totalSize)
                return 0; // The message body has not been fully received yet.
            // Parse the message body straight out of the buffer. This copies no payload bytes and
            // allocates no intermediate stream, and reads exactly the message's own bytes, so a
            // following message in the same buffer is left untouched.
            message = parser.ParseFrom (data, offset + prefixLength, size);
            return totalSize;
        }

        /// <summary>
        /// Decode the base-128 varint that prefixes a length-delimited message. Returns the message
        /// length in bytes and sets prefixLength to the number of bytes the varint occupies, or sets
        /// prefixLength to zero if the buffer does not yet contain the complete varint.
        /// </summary>
        static int DecodeMessageLength (byte[] data, int offset, int length, out int prefixLength)
        {
            var result = 0;
            // A varint encoding a 32-bit value is at most five bytes long.
            for (var i = 0; i < 5; i++) {
                if (i >= length) {
                    prefixLength = 0;
                    return 0;
                }
                int b = data [offset + i];
                result |= (b & 0x7f) << (7 * i);
                if ((b & 0x80) == 0) {
                    if (result < 0)
                        throw new InvalidOperationException ("Message length prefix is out of range");
                    prefixLength = i + 1;
                    return result;
                }
            }
            throw new InvalidOperationException ("Message length prefix is not a valid varint");
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
