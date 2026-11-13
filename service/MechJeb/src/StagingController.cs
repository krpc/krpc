using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The staging controller, which stages when a stage runs out of fuel.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class StagingController : KRPCComputerModule
    {
        static Field<object, double> autostagePreDelay;
        static Field<object, double> autostagePostDelay;
        static Field<object, int> autostageLimit;
        static Field<object, double> fairingMaxDynamicPressure;
        static Field<object, double> fairingMinAltitude;
        static Field<object, double> clampAutoStageThrustPct;
        static Field<object, double> fairingMaxAerothermalFlux;
        static Field<object, bool> hotStaging;
        static Field<object, double> hotStagingLeadTime;
        static Field<object, bool> autostagingOnce;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleStagingController");
            autostagePreDelay = Bindings.EditableDouble (type, "AutostagePreDelay");
            autostagePostDelay = Bindings.EditableDouble (type, "AutostagePostDelay");
            autostageLimit = Bindings.EditableInt (type, "AutostageLimit");
            fairingMaxDynamicPressure = Bindings.EditableDouble (type, "FairingMaxDynamicPressure");
            fairingMinAltitude = Bindings.EditableDouble (type, "FairingMinAltitude");
            clampAutoStageThrustPct = Bindings.EditableDouble (type, "ClampAutoStageThrustPct");
            fairingMaxAerothermalFlux = Bindings.EditableDouble (type, "FairingMaxAerothermalFlux");
            hotStaging = type.Field<object, bool> ("HotStaging");
            hotStagingLeadTime = Bindings.EditableDouble (type, "HotStagingLeadTime");
            autostagingOnce = type.Field<object, bool> ("AutostagingOnce");
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
