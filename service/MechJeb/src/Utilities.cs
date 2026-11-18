using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Utilities window, which holds the autostaging and throttle settings, and deploys
    /// the solar panels and antennas.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class Utilities
    {
        internal Utilities ()
        {
            Staging = new Autostaging ();
            Thrust = new ThrustLimiter ();
            SolarPanels = new DeployableController ("MechJebModuleSolarPanelController");
            Antennas = new DeployableController ("MechJebModuleDeployableAntennaController");
        }

        /// <summary>
        /// The autostaging settings. The same object as <see cref="AscentSettings.Staging"/>.
        /// </summary>
        [KRPCProperty]
        public Autostaging Staging { get; private set; }

        /// <summary>
        /// The throttle limiters. The same object as <see cref="AscentSettings.Thrust"/>.
        /// </summary>
        [KRPCProperty]
        public ThrustLimiter Thrust { get; private set; }

        /// <summary>
        /// The solar panels.
        /// </summary>
        [KRPCProperty]
        public DeployableController SolarPanels { get; private set; }

        /// <summary>
        /// The deployable antennas.
        /// </summary>
        [KRPCProperty]
        public DeployableController Antennas { get; private set; }
    }
}
