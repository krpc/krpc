using KRPC.Service.Attributes;

namespace KRPC.KerbalEngineerRedux
{
    /// <summary>
    /// Angles and times for a transfer to the vessel's target.
    /// </summary>
    /// <remarks>
    /// When the vessel and target orbit different bodies, these are for the orbits of the bodies
    /// that share a parent.
    /// </remarks>
    [KRPCStruct (Service = "KerbalEngineerRedux")]
    public struct Rendezvous
    {
        /// <summary>
        /// The angle from the vessel to the target, around the vessel's orbit, in radians.
        /// </summary>
        /// <remarks>
        /// Negative when the target's orbit is lower.
        /// </remarks>
        [KRPCProperty]
        public double PhaseAngle { get; set; }

        /// <summary>
        /// The phase angle at which to start a Hohmann transfer to the target, in radians.
        /// </summary>
        [KRPCProperty]
        public double InterceptAngle { get; set; }

        /// <summary>
        /// The time until the phase angle equals the intercept angle, in seconds.
        /// </summary>
        /// <remarks>
        /// Negative for the 20 degrees of phase after they pass.
        /// </remarks>
        [KRPCProperty]
        public double TimeToInterceptAngle { get; set; }

        /// <summary>
        /// The angle to the ascending node with the target's orbit, in radians.
        /// </summary>
        /// <remarks>
        /// When the vessel or target is landed, the rotation until the launch site passes under
        /// the ascending node.
        /// </remarks>
        [KRPCProperty]
        public double AngleToAscendingNode { get; set; }

        /// <summary>
        /// The angle to the descending node with the target's orbit, in radians.
        /// </summary>
        /// <remarks>
        /// When the vessel or target is landed, the rotation until the launch site passes under
        /// the descending node.
        /// </remarks>
        [KRPCProperty]
        public double AngleToDescendingNode { get; set; }

        /// <summary>
        /// The time until the ascending node with the target's orbit, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TimeToAscendingNode { get; set; }

        /// <summary>
        /// The time until the descending node with the target's orbit, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TimeToDescendingNode { get; set; }
    }
}
