using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Google.Protobuf;
using KRPC.Utils;

namespace KRPC.Server.ProtocolBuffers
{
    static class Utils
    {
        /// <summary>
        /// Read a message from the client.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidMethodWithUnusedGenericTypeRule")]
        public static T ReadMessage<T> (IStream<byte,byte> stream) where T : class, IMessage, new()
        {
            if (!stream.DataAvailable)
                return null;

            byte[] buffer = new byte[4096]; //TODO: sensible default???
            var data = new DynamicBuffer ();

            int read = stream.Read (buffer, 0, buffer.Length);
            if (read == 0)
                return null;
            data.Append (buffer, 0, read);

            var codedStream = new CodedInputStream (data.GetBuffer (), 0, data.Length);
            // Get the protobuf message size
            var size = (int)codedStream.ReadUInt32 ();
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
        /// Write a message
        /// </summary>
        public static void WriteMessage (IStream<byte,byte> stream, IMessage message)
        {
            var codedOutputStream = new CodedOutputStream (new ByteOutputAdapterStream (stream), true);
            codedOutputStream.WriteMessage (message);
            codedOutputStream.Flush ();
        }
    }
}
