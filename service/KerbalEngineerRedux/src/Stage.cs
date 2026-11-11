using KRPC.Service.Attributes;

namespace KRPC.KerbalEngineerRedux
{
    /// <summary>
    /// A stage of the vessel, from the simulation of Kerbal Engineer Redux.
    /// </summary>
    [KRPCStruct (Service = "KerbalEngineerRedux")]
    public struct Stage
    {
        /// <summary>
        /// The stage number.
        /// </summary>
        [KRPCProperty]
        public int Number { get; set; }

        /// <summary>
        /// The delta-v of the stage, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double DeltaV { get; set; }

        /// <summary>
        /// The delta-v of the stage and the stages after it, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double TotalDeltaV { get; set; }

        /// <summary>
        /// The specific impulse of the stage, in seconds.
        /// </summary>
        [KRPCProperty]
        public double SpecificImpulse { get; set; }

        /// <summary>
        /// The thrust of the stage at the current throttle, in Newtons.
        /// </summary>
        [KRPCProperty]
        public double Thrust { get; set; }

        /// <summary>
        /// The thrust of the stage at full throttle, in Newtons.
        /// </summary>
        [KRPCProperty]
        public double AvailableThrust { get; set; }

        /// <summary>
        /// The thrust-to-weight ratio at the start of the stage, at the current throttle.
        /// </summary>
        [KRPCProperty]
        public double TWR { get; set; }

        /// <summary>
        /// The thrust-to-weight ratio at the start of the stage, at full throttle.
        /// </summary>
        [KRPCProperty]
        public double AvailableTWR { get; set; }

        /// <summary>
        /// The highest thrust-to-weight ratio during the stage, at full throttle.
        /// </summary>
        [KRPCProperty]
        public double MaxTWR { get; set; }

        /// <summary>
        /// The mass of the vessel at the start of the stage, in kilograms.
        /// </summary>
        [KRPCProperty]
        public double StartMass { get; set; }

        /// <summary>
        /// The mass the vessel loses over the stage, in kilograms.
        /// </summary>
        /// <remarks>
        /// The resources the stage burns and the parts it drops.
        /// </remarks>
        [KRPCProperty]
        public double MassLost { get; set; }

        /// <summary>
        /// The mass of the resources the stage burns, in kilograms.
        /// </summary>
        [KRPCProperty]
        public double ResourceMass { get; set; }

        /// <summary>
        /// The burn time of the stage at full throttle, in seconds.
        /// </summary>
        [KRPCProperty]
        public double BurnTime { get; set; }

        /// <summary>
        /// The burn time of the stage and the stages after it, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TotalBurnTime { get; set; }

        /// <summary>
        /// The torque from the thrust at full throttle about the center of mass at the start
        /// of the stage, in <unit>N.m</unit>.
        /// </summary>
        [KRPCProperty]
        public double AvailableThrustTorque { get; set; }

        /// <summary>
        /// The angle between the thrust and the direction to the center of mass, in degrees.
        /// </summary>
        [KRPCProperty]
        public double ThrustOffsetAngle { get; set; }

        /// <summary>
        /// The specific impulse of the RCS thrusters, in seconds.
        /// </summary>
        [KRPCProperty]
        public double RCSSpecificImpulse { get; set; }

        /// <summary>
        /// The thrust of the RCS thrusters, in Newtons.
        /// </summary>
        [KRPCProperty]
        public double RCSThrust { get; set; }

        /// <summary>
        /// The RCS delta-v at the start of the stage, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double RCSStartDeltaV { get; set; }

        /// <summary>
        /// The RCS delta-v at the end of the stage, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double RCSEndDeltaV { get; set; }

        /// <summary>
        /// The RCS thrust-to-weight ratio at the start of the stage.
        /// </summary>
        [KRPCProperty]
        public double RCSStartTWR { get; set; }

        /// <summary>
        /// The RCS thrust-to-weight ratio at the end of the stage.
        /// </summary>
        [KRPCProperty]
        public double RCSEndTWR { get; set; }

        /// <summary>
        /// The burn time of the RCS thrusters, in seconds.
        /// </summary>
        [KRPCProperty]
        public double RCSBurnTime { get; set; }

        /// <summary>
        /// The cost of the vessel at the start of the stage.
        /// </summary>
        [KRPCProperty]
        public double StartCost { get; set; }

        /// <summary>
        /// The cost the vessel loses over the stage.
        /// </summary>
        /// <remarks>
        /// The resources the stage burns and the parts it drops.
        /// </remarks>
        [KRPCProperty]
        public double CostLost { get; set; }

        /// <summary>
        /// The number of parts at the start of the stage.
        /// </summary>
        [KRPCProperty]
        public int StartPartCount { get; set; }

        /// <summary>
        /// The number of parts the stage drops.
        /// </summary>
        [KRPCProperty]
        public int DroppedPartCount { get; set; }
    }
}
