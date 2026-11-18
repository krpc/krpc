using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// The performance statistics of the server.
    /// </summary>
    /// <remarks>
    /// See <see cref="ServerStatus.Statistics"/>.
    /// </remarks>
    [KRPCStruct (Service = "KRPC")]
    public struct ServerStatistics
    {
        /// <summary>
        /// The number of bytes received from clients.
        /// </summary>
        [KRPCProperty]
        public ulong BytesRead { get; set; }

        /// <summary>
        /// The number of bytes sent to clients.
        /// </summary>
        [KRPCProperty]
        public ulong BytesWritten { get; set; }

        /// <summary>
        /// The rate bytes are received from clients, in bytes per second.
        /// </summary>
        [KRPCProperty]
        public float BytesReadRate { get; set; }

        /// <summary>
        /// The rate bytes are sent to clients, in bytes per second.
        /// </summary>
        [KRPCProperty]
        public float BytesWrittenRate { get; set; }

        /// <summary>
        /// The number of calls executed.
        /// </summary>
        [KRPCProperty]
        public ulong RpcsExecuted { get; set; }

        /// <summary>
        /// The number of calls executed per second.
        /// </summary>
        [KRPCProperty]
        public float RpcRate { get; set; }

        /// <summary>
        /// The game's frame rate, in frames per second.
        /// </summary>
        [KRPCProperty]
        public float FrameRate { get; set; }

        /// <summary>
        /// The server's time per frame, in seconds: executing calls, waiting for them and
        /// updating streams.
        /// </summary>
        [KRPCProperty]
        public float TimePerFrame { get; set; }

        /// <summary>
        /// The time spent executing calls per frame, in seconds.
        /// </summary>
        [KRPCProperty]
        public float ExecTimePerFrame { get; set; }

        /// <summary>
        /// The time spent waiting for calls per frame, in seconds.
        /// </summary>
        [KRPCProperty]
        public float WaitTimePerFrame { get; set; }

        /// <summary>
        /// The time spent updating streams per frame, in seconds.
        /// </summary>
        [KRPCProperty]
        public float StreamTimePerFrame { get; set; }

        /// <summary>
        /// The time the server may spend per frame, in seconds.
        /// </summary>
        /// <remarks>
        /// Adaptive rate control adjusts it, up to <see cref="ServerSettings.MaxTimePerFrame"/>.
        /// </remarks>
        [KRPCProperty]
        public float BudgetPerFrame { get; set; }

        /// <summary>
        /// The median time between a response and the client's next call, over the
        /// clients' recent calls, in seconds.
        /// </summary>
        [KRPCProperty]
        public float MedianTurnaround { get; set; }

        /// <summary>
        /// The number of calls left for the next update, because the update ran out of time
        /// or the call yielded.
        /// </summary>
        [KRPCProperty]
        public uint DeferredCalls { get; set; }

        /// <summary>
        /// The number of open streams, events included.
        /// </summary>
        [KRPCProperty]
        public uint Streams { get; set; }

        /// <summary>
        /// The number of stream evaluations, one per stream per update.
        /// </summary>
        [KRPCProperty]
        public ulong StreamEvaluations { get; set; }

        /// <summary>
        /// The number of stream evaluations per second.
        /// </summary>
        [KRPCProperty]
        public float StreamEvaluationRate { get; set; }

        /// <summary>
        /// The number of stream updates sent to clients.
        /// </summary>
        [KRPCProperty]
        public ulong StreamUpdatesSent { get; set; }
    }
}
