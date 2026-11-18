using System;
using KRPC.MechJeb.Maneuver;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// This service provides functionality to interact with MechJeb 2.
    /// </summary>
    /// <remarks>
    /// The objects act on the MechJeb of the active vessel, and stay valid after the active
    /// vessel changes.
    /// </remarks>
    [KRPCService (Id = ServiceInfo.Id, GameScene = GameScene.Flight)]
    [RequiresMod (typeof (Bindings))]
    public static class MechJeb
    {
        static Func<global::Vessel, object> getMasterMechJeb;
        static Func<object, string, object> getComputerModule;

        static readonly AircraftApproach aircraftApproach = new AircraftApproach ();
        static readonly AircraftAutopilot aircraftAutopilot = new AircraftAutopilot ();
        static readonly AscentGuidance ascentGuidance = new AscentGuidance ();
        static readonly AttitudeAdjustment attitudeAdjustment = new AttitudeAdjustment ();
        static readonly DockingAutopilot dockingAutopilot = new DockingAutopilot ();
        static readonly LandingGuidance landingGuidance = new LandingGuidance ();
        static readonly RendezvousAutopilot rendezvousAutopilot = new RendezvousAutopilot ();
        static readonly RoverAutopilot roverAutopilot = new RoverAutopilot ();
        static readonly ManeuverPlanner maneuverPlanner = new ManeuverPlanner ();
        static readonly SmartASS smartASS = new SmartASS ();
        static readonly SmartRCS smartRCS = new SmartRCS ();
        static readonly Translatron translatron = new Translatron ();
        static readonly TargetController targetController = new TargetController ();
        static readonly Utilities utilities = new Utilities ();

        internal static void Load ()
        {
            var core = Bindings.Type ("MuMech.MechJebCore");
            var extensions = Bindings.Type ("MuMech.VesselExtensions");
            getMasterMechJeb = extensions.Method<Func<global::Vessel, object>> ("GetMasterMechJeb");
            getComputerModule = core.Method<Func<object, string, object>> ("GetComputerModule");
            ComputerModule.Load ();
            AutopilotModule.Load ();
            VesselState.Load (core);
            AircraftApproach.Load ();
            AircraftAutopilot.Load ();
            AscentGuidance.Load ();
            AscentSettings.Load ();
            AttitudeAdjustment.Load ();
            Autostaging.Load ();
            ClassicPath.Load ();
            DeployableController.Load ();
            DockingAutopilot.Load ();
            LandingGuidance.Load ();
            ManeuverPlanner.Load ();
            NodeExecutor.Load ();
            PVGSettings.Load ();
            RendezvousAutopilot.Load ();
            RoverAutopilot.Load ();
            SmartASS.Load ();
            SmartRCS.Load ();
            TargetController.Load ();
            ThrustLimiter.Load ();
            Translatron.Load ();
            Operation.Load ();
            TimeSelector.Load ();
        }

        /// <summary>
        /// The MechJeb core of the active vessel, or null if it has none.
        /// </summary>
        static object MasterMechJeb {
            get {
                var vessel = FlightGlobals.ActiveVessel;
                return vessel != null ? getMasterMechJeb (vessel) : null;
            }
        }

        /// <summary>
        /// The MechJeb core of the active vessel. Throws an exception if it has none.
        /// </summary>
        internal static object Core {
            get {
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

        /// <summary>
        /// Whether MechJeb is installed.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.All)]
        public static bool Available {
            get { return ModAPI.Get<Bindings> ().IsAvailable; }
        }

        /// <summary>
        /// Whether the active vessel has MechJeb.
        /// </summary>
        [KRPCProperty]
        public static bool Ready {
            get { return MasterMechJeb != null; }
        }

        /// <summary>
        /// The Aircraft Approach and Autoland window.
        /// </summary>
        [KRPCProperty]
        public static AircraftApproach AircraftApproach {
            get { return aircraftApproach; }
        }

        /// <summary>
        /// The Aircraft Autopilot window.
        /// </summary>
        [KRPCProperty]
        public static AircraftAutopilot AircraftAutopilot {
            get { return aircraftAutopilot; }
        }

        /// <summary>
        /// The Ascent Guidance window.
        /// </summary>
        [KRPCProperty]
        public static AscentGuidance AscentGuidance {
            get { return ascentGuidance; }
        }

        /// <summary>
        /// The Attitude Adjustment window.
        /// </summary>
        [KRPCProperty]
        public static AttitudeAdjustment AttitudeAdjustment {
            get { return attitudeAdjustment; }
        }

        /// <summary>
        /// The Docking Autopilot window.
        /// </summary>
        [KRPCProperty]
        public static DockingAutopilot DockingAutopilot {
            get { return dockingAutopilot; }
        }

        /// <summary>
        /// The Landing Guidance window.
        /// </summary>
        [KRPCProperty]
        public static LandingGuidance LandingGuidance {
            get { return landingGuidance; }
        }

        /// <summary>
        /// The Maneuver Planner window.
        /// </summary>
        [KRPCProperty]
        public static ManeuverPlanner ManeuverPlanner {
            get { return maneuverPlanner; }
        }

        /// <summary>
        /// The Rendezvous Autopilot window.
        /// </summary>
        [KRPCProperty]
        public static RendezvousAutopilot RendezvousAutopilot {
            get { return rendezvousAutopilot; }
        }

        /// <summary>
        /// The Rover Autopilot window.
        /// </summary>
        [KRPCProperty]
        public static RoverAutopilot RoverAutopilot {
            get { return roverAutopilot; }
        }

        /// <summary>
        /// The Smart A.S.S. window.
        /// </summary>
        [KRPCProperty]
        public static SmartASS SmartASS {
            get { return smartASS; }
        }

        /// <summary>
        /// The Smart RCS window.
        /// </summary>
        [KRPCProperty]
        public static SmartRCS SmartRCS {
            get { return smartRCS; }
        }

        /// <summary>
        /// The target controller, which holds MechJeb's target.
        /// </summary>
        [KRPCProperty]
        public static TargetController TargetController {
            get { return targetController; }
        }

        /// <summary>
        /// The Translatron window.
        /// </summary>
        [KRPCProperty]
        public static Translatron Translatron {
            get { return translatron; }
        }

        /// <summary>
        /// The Utilities window.
        /// </summary>
        [KRPCProperty]
        public static Utilities Utilities {
            get { return utilities; }
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
