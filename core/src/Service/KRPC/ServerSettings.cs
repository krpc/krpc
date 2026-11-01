using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// The settings that control how the server executes calls.
    /// </summary>
    /// <remarks>
    /// See <see cref="ServerStatus.Settings"/>.
    /// </remarks>
    [KRPCStruct (Service = "KRPC")]
    public struct ServerSettings
    {
        /// <summary>
        /// Whether the server executes one call per update.
        /// </summary>
        [KRPCProperty]
        public bool OneRpcPerUpdate { get; set; }

        /// <summary>
        /// The maximum time spent executing calls per update, in microseconds.
        /// </summary>
        [KRPCProperty]
        public uint MaxTimePerUpdate { get; set; }

        /// <summary>
        /// Whether the server adjusts <see cref="MaxTimePerUpdate"/> to hold the frame rate.
        /// </summary>
        [KRPCProperty]
        public bool AdaptiveRateControl { get; set; }

        /// <summary>
        /// Whether the server waits for the next call in an update.
        /// </summary>
        [KRPCProperty]
        public bool BlockingRecv { get; set; }

        /// <summary>
        /// The time the server waits for the next call, in microseconds.
        /// </summary>
        [KRPCProperty]
        public uint RecvTimeout { get; set; }

        /// <summary>
        /// The longest time a client may hold the game on one physics tick, in microseconds.
        /// </summary>
        /// <remarks>
        /// See <see cref="KRPC.HoldTick"/>.
        /// </remarks>
        [KRPCProperty]
        public uint TickHoldTimeout { get; set; }
    }
}
