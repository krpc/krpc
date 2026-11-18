using System;

namespace KRPC.Server
{
    /// <summary>
    /// A byte stream read by a thread of its own, which can run a handler on that thread as
    /// bytes arrive.
    /// </summary>
    interface IReaderThreadStream
    {
        /// <summary>
        /// Run on the reader thread each time bytes arrive, before the server's wake event is
        /// set, so that the work it does is done by the time the game thread wakes.
        /// </summary>
        Action Received { set; }
    }
}
