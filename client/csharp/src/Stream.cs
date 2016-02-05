using Google.Protobuf;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Schema.KRPC;
using System;

namespace KRPC.Client
{
    /// <summary>
    /// Object representing a stream.
    /// </summary>
    public class Stream<ReturnType>
    {
        StreamManager streamManager;

        internal UInt32 Id { get; private set; }

        internal Stream (Connection connection, Request request)
        {
            streamManager = connection.StreamManager;
            Id = streamManager.AddStream (request, typeof(ReturnType));
        }

        /// <summary>
        /// Get the most recent version of the stream.
        /// </summary>
        public ReturnType Get ()
        {
            return (ReturnType)streamManager.GetValue (Id);
        }

        /// <summary>
        /// Remove the stream from the server.
        /// </summary>
        public void Remove ()
        {
            streamManager.RemoveStream (Id);
        }
    }
}
