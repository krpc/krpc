using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The staging controller, which stages when a stage runs out of fuel.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class StagingController : KRPCComputerModule
    {
        static Value<double> autostagePreDelay;
        static Value<double> autostagePostDelay;
        static Value<int> autostageLimit;
        static Value<double> fairingMaxDynamicPressure;
        static Value<double> fairingMinAltitude;
        static Value<double> clampAutoStageThrustPct;
        static Value<double> fairingMaxAerothermalFlux;
        static Value<bool> hotStaging;
        static Value<double> hotStagingLeadTime;
        static Value<bool> autostagingOnce;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleStagingController");
            autostagePreDelay = Reflection.EditableDouble (type, "AutostagePreDelay");
            autostagePostDelay = Reflection.EditableDouble (type, "AutostagePostDelay");
            autostageLimit = Reflection.EditableInt (type, "AutostageLimit");
            fairingMaxDynamicPressure = Reflection.EditableDouble (type, "FairingMaxDynamicPressure");
            fairingMinAltitude = Reflection.EditableDouble (type, "FairingMinAltitude");
            clampAutoStageThrustPct = Reflection.EditableDouble (type, "ClampAutoStageThrustPct");
            fairingMaxAerothermalFlux = Reflection.EditableDouble (type, "FairingMaxAerothermalFlux");
            hotStaging = Reflection.Field<bool> (type, "HotStaging");
            hotStagingLeadTime = Reflection.EditableDouble (type, "HotStagingLeadTime");
            autostagingOnce = Reflection.Field<bool> (type, "AutostagingOnce");
        }

        internal StagingController () : base ("MechJebModuleStagingController")
        {
        }

        /// <summary>
        /// The delay before each staging, in seconds.
        /// </summary>
        [KRPCProperty]
        public double AutostagePreDelay {
            get { return autostagePreDelay.Get (Instance); }
            set { autostagePreDelay.Set (Instance, value); }
        }

        /// <summary>
        /// The delay after each staging, in seconds.
        /// </summary>
        [KRPCProperty]
        public double AutostagePostDelay {
            get { return autostagePostDelay.Get (Instance); }
            set { autostagePostDelay.Set (Instance, value); }
        }

        /// <summary>
        /// The last stage the controller activates.
        /// </summary>
        [KRPCProperty]
        public int AutostageLimit {
            get { return autostageLimit.Get (Instance); }
            set { autostageLimit.Set (Instance, value); }
        }

        /// <summary>
        /// The largest dynamic pressure at which the controller jettisons a fairing, in pascals.
        /// </summary>
        [KRPCProperty]
        public double FairingMaxDynamicPressure {
            get { return fairingMaxDynamicPressure.Get (Instance); }
            set { fairingMaxDynamicPressure.Set (Instance, value); }
        }

        /// <summary>
        /// The lowest altitude at which the controller jettisons a fairing, in meters.
        /// </summary>
        [KRPCProperty]
        public double FairingMinAltitude {
            get { return fairingMinAltitude.Get (Instance); }
            set { fairingMinAltitude.Set (Instance, value); }
        }

        /// <summary>
        /// The fraction of full thrust at which the controller releases the launch clamps.
        /// </summary>
        [KRPCProperty]
        public double ClampAutoStageThrustPct {
            get { return clampAutoStageThrustPct.Get (Instance); }
            set { clampAutoStageThrustPct.Set (Instance, value); }
        }

        /// <summary>
        /// The largest aerothermal flux at which the controller jettisons a fairing, in
        /// <unit>W/m^2</unit>.
        /// </summary>
        [KRPCProperty]
        public double FairingMaxAerothermalFlux {
            get { return fairingMaxAerothermalFlux.Get (Instance); }
            set { fairingMaxAerothermalFlux.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the controller ignites the next stage before the current one burns out.
        /// </summary>
        [KRPCProperty]
        public bool HotStaging {
            get { return hotStaging.Get (Instance); }
            set { hotStaging.Set (Instance, value); }
        }

        /// <summary>
        /// The time before burnout at which a hot staging happens, in seconds.
        /// </summary>
        /// <remarks>Requires <see cref="HotStaging"/>.</remarks>
        [KRPCProperty]
        public double HotStagingLeadTime {
            get { return hotStagingLeadTime.Get (Instance); }
            set { hotStagingLeadTime.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the controller disables itself after one staging.
        /// </summary>
        /// <remarks>Requires the controller to be enabled.</remarks>
        [KRPCProperty]
        public bool AutostagingOnce {
            get { return autostagingOnce.Get (Instance); }
            set { autostagingOnce.Set (Instance, value); }
        }
    }
}
