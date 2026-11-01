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
        /// The time spent receiving and executing calls per update, in seconds.
        /// </summary>
        [KRPCProperty]
        public float TimePerRpcUpdate { get; set; }

        /// <summary>
        /// The time spent receiving calls per update, in seconds.
        /// </summary>
        [KRPCProperty]
        public float PollTimePerRpcUpdate { get; set; }

        /// <summary>
        /// The time spent executing calls per update, in seconds.
        /// </summary>
        [KRPCProperty]
        public float ExecTimePerRpcUpdate { get; set; }

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

        /// <summary>
        /// The time spent updating streams per update, in seconds.
        /// </summary>
        [KRPCProperty]
        public float TimePerStreamUpdate { get; set; }
    }
}
