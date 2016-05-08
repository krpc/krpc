using System;
using KRPC.Client;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    /// <summary>
    /// Object representing a stream.
    /// </summary>
    public class Stream<ReturnType>
    {
        readonly StreamManager streamManager;

        internal UInt32 Id { get; private set; }

        internal Stream (Connection connection, Request request)
        {
            streamManager = connection.StreamManager;
            Id = streamManager.AddStream (request, typeof(ReturnType));
        }

        /// <summary>
        /// Get the most recent value of the stream.
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
