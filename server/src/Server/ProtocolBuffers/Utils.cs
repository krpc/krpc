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
        /// Read a data from the client, up to the given timeout
        /// </summary>
        public static void Read (IStream<byte,byte> stream, byte[] buffer, float timeout = 1)
        {
            Stopwatch timer = Stopwatch.StartNew ();
            int offset = 0;
            while (timer.ElapsedSeconds () < timeout)
            {
                if (stream.DataAvailable) {
                    offset += stream.Read (buffer, offset);
                    if (offset == buffer.Length)
                        return;
                }
                System.Threading.Thread.Sleep (50);
            }
            throw new TimeoutException();
        }

        /// <summary>
        /// Read a message from the client, up to the given timeout
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidMethodWithUnusedGenericTypeRule")]
        public static T ReadMessage<T> (IStream<byte,byte> stream, float timeout = 1) where T : IMessage, new()
        {
            byte[] buffer = new byte[4096]; //TODO: sensible default???
            var data = new DynamicBuffer ();

            Stopwatch timer = Stopwatch.StartNew ();
            while (timer.ElapsedSeconds () < timeout)
            {
                if (stream.DataAvailable)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read == 0)
                        continue;
                    data.Append(buffer, 0, read);

                    var codedStream = new CodedInputStream (data.GetBuffer(), 0, data.Length);
                    // Get the protobuf message size
                    int size = (int)codedStream.ReadUInt32 ();
                    int totalSize = (int)codedStream.Position + size;
                    // Check if enough data is available, if not then delay the decoding
                    if (data.Length < totalSize)
                        continue;
                    // Decode the request
                    var message = new T ();
                    message.MergeFrom (codedStream);
                    return message;
                }
                System.Threading.Thread.Sleep (50);
            }
            throw new TimeoutException();
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
