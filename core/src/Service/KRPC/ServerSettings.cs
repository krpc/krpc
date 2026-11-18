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
        /// Whether the server adapts its time per frame to hold the frame rate at
        /// <see cref="TargetFrameRate"/>.
        /// </summary>
        [KRPCProperty]
        public bool AdaptiveRateControl { get; set; }

        /// <summary>
        /// The lowest frame rate the server's work pushes the game to, in frames per second.
        /// </summary>
        [KRPCProperty]
        public uint TargetFrameRate { get; set; }

        /// <summary>
        /// The server's budget per frame, or its ceiling with adaptive rate control, in microseconds.
        /// </summary>
        [KRPCProperty]
        public uint MaxTimePerFrame { get; set; }

        /// <summary>
        /// The longest the server waits for a client's next call, in microseconds.
        /// </summary>
        [KRPCProperty]
        public uint MaxCallWait { get; set; }

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
