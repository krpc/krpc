using System;
using KRPC.MechJeb.Maneuver;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// This service provides functionality to interact with MechJeb 2.
    /// </summary>
    /// <remarks>
    /// The objects act on the MechJeb of the active vessel, and stay valid after the active
    /// vessel changes.
    /// </remarks>
    [KRPCService (GameScene = GameScene.Flight)]
    public static class MechJeb
    {
        static Func<global::Vessel, object> getMasterMechJeb;
        static Func<object, string, object> getComputerModule;

        static readonly AirplaneAutopilot airplaneAutopilot = new AirplaneAutopilot ();
        static readonly AscentAutopilot ascentAutopilot = new AscentAutopilot ();
        static readonly DockingAutopilot dockingAutopilot = new DockingAutopilot ();
        static readonly LandingAutopilot landingAutopilot = new LandingAutopilot ();
        static readonly RendezvousAutopilot rendezvousAutopilot = new RendezvousAutopilot ();
        static readonly ManeuverPlanner maneuverPlanner = new ManeuverPlanner ();
        static readonly SmartASS smartASS = new SmartASS ();
        static readonly SmartRCS smartRCS = new SmartRCS ();
        static readonly Translatron translatron = new Translatron ();
        static readonly DeployableController antennaController = new DeployableController ("MechJebModuleDeployableAntennaController");
        static readonly NodeExecutor nodeExecutor = new NodeExecutor ();
        static readonly RCSController rcsController = new RCSController ();
        static readonly StagingController stagingController = new StagingController ();
        static readonly DeployableController solarPanelController = new DeployableController ("MechJebModuleSolarPanelController");
        static readonly TargetController targetController = new TargetController ();
        static readonly ThrustController thrustController = new ThrustController ();

        /// <summary>
        /// Whether MechJeb is installed, and its members bound.
        /// </summary>
        internal static bool Loaded { get; private set; }

        internal static void Load ()
        {
            Reflection.Load ();
            var core = Reflection.Type ("MuMech.MechJebCore");
            var extensions = Reflection.Type ("MuMech.VesselExtensions");
            if (core == null || extensions == null) {
                Loaded = false;
                return;
            }
            getMasterMechJeb = Reflection.Method<Func<global::Vessel, object>> (extensions, "GetMasterMechJeb");
            getComputerModule = Reflection.Method<Func<object, string, object>> (core, "GetComputerModule");
            ComputerModule.Load ();
            AutopilotModule.Load ();
            VesselState.Load (core);
            AirplaneAutopilot.Load ();
            AscentAutopilot.Load ();
            AscentClassic.Load ();
            AscentPVG.Load ();
            DeployableController.Load ();
            DockingAutopilot.Load ();
            LandingAutopilot.Load ();
            ManeuverPlanner.Load ();
            NodeExecutor.Load ();
            RCSController.Load ();
            RendezvousAutopilot.Load ();
            SmartASS.Load ();
            SmartRCS.Load ();
            StagingController.Load ();
            TargetController.Load ();
            ThrustController.Load ();
            Translatron.Load ();
            Operation.Load ();
            TimeSelector.Load ();
            Reflection.LogMissing ();
            Loaded = true;
        }

        /// <summary>
        /// The MechJeb core of the active vessel, or null if it has none.
        /// </summary>
        static object MasterMechJeb {
            get {
                var vessel = FlightGlobals.ActiveVessel;
                return Loaded && vessel != null ? getMasterMechJeb (vessel) : null;
            }
        }

        internal static void CheckAPI ()
        {
            if (!Loaded)
                throw new InvalidOperationException ("MechJeb is not available");
        }

        /// <summary>
        /// The MechJeb core of the active vessel. Throws an exception if it has none.
        /// </summary>
        internal static object Core {
            get {
                CheckAPI ();
                var core = MasterMechJeb;
                if (core == null)
                    throw new InvalidOperationException ("The active vessel has no MechJeb");
                return core;
            }
        }

        /// <summary>
        /// The computer module of the given type name, of the active vessel's MechJeb.
        /// </summary>
        internal static object Module (string name)
        {
            var module = getComputerModule (Core, name);
            if (module == null)
                throw new MJServiceException ("MechJeb module " + name + " not found");
            return module;
        }

        static T Checked<T> (T module)
        {
            CheckAPI ();
            return module;
        }

        /// <summary>
        /// Whether MechJeb is installed and the active vessel has MechJeb.
        /// </summary>
        [KRPCProperty]
        public static bool APIReady {
            get { return MasterMechJeb != null; }
        }

        /// <summary>
        /// The airplane autopilot.
        /// </summary>
        [KRPCProperty]
        public static AirplaneAutopilot AirplaneAutopilot {
            get { return Checked (airplaneAutopilot); }
        }

        /// <summary>
        /// The ascent autopilot.
        /// </summary>
        [KRPCProperty]
        public static AscentAutopilot AscentAutopilot {
            get { return Checked (ascentAutopilot); }
        }

        /// <summary>
        /// The docking autopilot.
        /// </summary>
        [KRPCProperty]
        public static DockingAutopilot DockingAutopilot {
            get { return Checked (dockingAutopilot); }
        }

        /// <summary>
        /// The landing autopilot.
        /// </summary>
        [KRPCProperty]
        public static LandingAutopilot LandingAutopilot {
            get { return Checked (landingAutopilot); }
        }

        /// <summary>
        /// The rendezvous autopilot.
        /// </summary>
        [KRPCProperty]
        public static RendezvousAutopilot RendezvousAutopilot {
            get { return Checked (rendezvousAutopilot); }
        }

        /// <summary>
        /// The maneuver planner.
        /// </summary>
        [KRPCProperty]
        public static ManeuverPlanner ManeuverPlanner {
            get { return Checked (maneuverPlanner); }
        }

        /// <summary>
        /// The Smart A.S.S. window.
        /// </summary>
        [KRPCProperty]
        public static SmartASS SmartASS {
            get { return Checked (smartASS); }
        }

        /// <summary>
        /// The Smart RCS window.
        /// </summary>
        [KRPCProperty]
        public static SmartRCS SmartRCS {
            get { return Checked (smartRCS); }
        }

        /// <summary>
        /// The Translatron window.
        /// </summary>
        [KRPCProperty]
        public static Translatron Translatron {
            get { return Checked (translatron); }
        }

        /// <summary>
        /// The controller of the deployable antennas.
        /// </summary>
        [KRPCProperty]
        public static DeployableController AntennaController {
            get { return Checked (antennaController); }
        }

        /// <summary>
        /// The maneuver node executor.
        /// </summary>
        [KRPCProperty]
        public static NodeExecutor NodeExecutor {
            get { return Checked (nodeExecutor); }
        }

        /// <summary>
        /// The RCS controller.
        /// </summary>
        [KRPCProperty]
        public static RCSController RCSController {
            get { return Checked (rcsController); }
        }

        /// <summary>
        /// The staging controller.
        /// </summary>
        [KRPCProperty]
        public static StagingController StagingController {
            get { return Checked (stagingController); }
        }

        /// <summary>
        /// The controller of the deployable solar panels.
        /// </summary>
        [KRPCProperty]
        public static DeployableController SolarPanelController {
            get { return Checked (solarPanelController); }
        }

        /// <summary>
        /// The target controller.
        /// </summary>
        [KRPCProperty]
        public static TargetController TargetController {
            get { return Checked (targetController); }
        }

        /// <summary>
        /// The thrust controller.
        /// </summary>
        [KRPCProperty]
        public static ThrustController ThrustController {
            get { return Checked (thrustController); }
        }
    }

    /// <summary>
    /// A general error in the service.
    /// </summary>
    [KRPCException (Service = "MechJeb")]
    public sealed class MJServiceException : Exception
    {
        /// <summary>
        /// Create the exception.
        /// </summary>
        public MJServiceException ()
        {
        }

        /// <summary>
        /// Create the exception with a message.
        /// </summary>
        public MJServiceException (string message) : base (message)
        {
        }

        /// <summary>
        /// Create the exception with a message and an inner exception.
        /// </summary>
        public MJServiceException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
