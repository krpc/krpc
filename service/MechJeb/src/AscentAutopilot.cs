using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The ascent autopilot.
    /// </summary>
    /// <remarks>
    /// See the <a href="https://github.com/MuMech/MechJeb2/wiki/Ascent-Guidance">MechJeb wiki</a>
    /// for how to set up this autopilot.
    /// </remarks>
    [KRPCClass (Service = "MechJeb")]
    public sealed class AscentAutopilot : KRPCComputerModule
    {
        const string settingsName = "MechJebModuleAscentSettings";
        const string classicName = "MechJebModuleAscentClassicAutopilot";
        const string pvgName = "MechJebModuleAscentPVGAutopilot";
        // The values of MechJeb's AscentType
        const int classic = 0;
        const int pvg = 1;

        static Func<object, object> autopilot;
        static Func<object, string> status;
        static Field<object, bool> timedLaunch;
        static Action<object, double> startCountdown;
        static Func<double, CelestialBody, double, global::Orbit, double> timeToPhaseAngle;
        static Func<double, double, double, double, double, double> timeToPlane;

        static Field<object, int> ascentType;
        static Field<object, double> desiredOrbitAltitude;
        static Field<object, double> desiredInclination;
        static Field<object, bool> correctiveSteering;
        static Field<object, double> correctiveSteeringGain;
        static Field<object, bool> forceRoll;
        static Field<object, double> verticalRoll;
        static Field<object, double> turnRoll;
        static Field<object, bool> autodeploySolarPanels;
        static Field<object, bool> autoDeployAntennas;
        static Field<object, bool> skipCircularization;
        static Field<object, bool> autostage;
        static Field<object, bool> limitAoA;
        static Field<object, double> maxAoA;
        static Field<object, double> aoaLimitFadeoutPressure;
        static Field<object, double> launchPhaseAngle;
        static Field<object, double> launchLANDifference;
        static Field<object, int> warpCountDown;
        static Field<object, bool> launchingToPlane;
        static Field<object, bool> launchingToRendezvous;
        static Field<object, bool> launchingToMatchLan;
        static Field<object, bool> launchingToLan;

        internal static new void Load ()
        {
            var settings = Bindings.Type ("MuMech." + settingsName);
            autopilot = settings.Getter<object, object> ("AscentAutopilot");
            var type = Bindings.Type ("MuMech.MechJebModuleAscentBaseAutopilot");
            status = type.Getter<object, string> ("Status");
            timedLaunch = type.Field<object, bool> ("TimedLaunch");
            startCountdown = type.Method<Action<object, double>> ("StartCountdown");
            timeToPhaseAngle = Bindings.Type ("MuMech.MechJebModuleAscentMenu").Method<Func<double, CelestialBody, double, global::Orbit, double>> ("TimeToPhaseAngle");
            timeToPlane = Bindings.Type ("MechJebLib.Functions.Astro").Method<Func<double, double, double, double, double, double>> ("TimeToPlane");

            ascentType = settings.Field<object, int> ("AscentType");
            desiredOrbitAltitude = Bindings.EditableDouble (settings, "DesiredOrbitAltitude");
            desiredInclination = Bindings.EditableDouble (settings, "DesiredInclination");
            correctiveSteering = settings.Field<object, bool> ("CorrectiveSteering");
            correctiveSteeringGain = Bindings.EditableDouble (settings, "CorrectiveSteeringGain");
            forceRoll = settings.Field<object, bool> ("ForceRoll");
            verticalRoll = Bindings.EditableDouble (settings, "VerticalRoll");
            turnRoll = Bindings.EditableDouble (settings, "TurnRoll");
            autodeploySolarPanels = settings.Field<object, bool> ("AutodeploySolarPanels");
            autoDeployAntennas = settings.Field<object, bool> ("AutoDeployAntennas");
            skipCircularization = settings.Field<object, bool> ("SkipCircularization");
            autostage = settings.Field<object, bool> ("Autostage");
            limitAoA = settings.Field<object, bool> ("LimitAoA");
            maxAoA = Bindings.EditableDouble (settings, "MaxAoA");
            aoaLimitFadeoutPressure = Bindings.EditableDouble (settings, "AOALimitFadeoutPressure");
            launchPhaseAngle = Bindings.EditableDouble (settings, "LaunchPhaseAngle");
            launchLANDifference = Bindings.EditableDouble (settings, "LaunchLANDifference");
            warpCountDown = Bindings.EditableInt (settings, "WarpCountDown");
            launchingToPlane = settings.Field<object, bool> ("LaunchingToPlane");
            launchingToRendezvous = settings.Field<object, bool> ("LaunchingToRendezvous");
            launchingToMatchLan = settings.Field<object, bool> ("LaunchingToMatchLan");
            launchingToLan = settings.Field<object, bool> ("LaunchingToLan");
        }

        internal AscentAutopilot () : base (settingsName)
        {
            AscentPathClassic = new AscentClassic ();
            AscentPathPVG = new AscentPVG ();
        }

        /// <summary>
        /// The ascent settings, which hold the settings of every ascent path.
        /// </summary>
        internal static object Settings {
            get { return MechJeb.Module (settingsName); }
        }

        /// <summary>
        /// The autopilot of the selected ascent path.
        /// </summary>
        internal override object Instance {
            get { return autopilot (Settings); }
        }

        /// <summary>
        /// Whether the autopilot is enabled. Enabling it engages the autopilot of the selected
        /// ascent path, and disabling it disengages the autopilots of both paths.
        /// </summary>
        [KRPCProperty]
        public override bool Enabled {
            get { return base.Enabled; }
            set {
                if (value) {
                    base.Enabled = true;
                } else {
                    // The path may have changed since the autopilot was enabled
                    SetEnabled (MechJeb.Module (classicName), false);
                    SetEnabled (MechJeb.Module (pvgName), false);
                }
            }
        }

        /// <summary>
        /// The autopilot status, which depends on the selected ascent path.
        /// </summary>
        [KRPCProperty]
        public string Status {
            get { return status (Instance); }
        }

        /// <summary>
        /// The selected ascent path: 0 for <see cref="AscentClassic"/> or 2 for
        /// <see cref="AscentPVG"/>.
        /// </summary>
        /// <remarks>
        /// MechJeb 2.15 removed the stock-style gravity turn path, which was 1. Setting any other
        /// value throws an exception.
        /// </remarks>
        [KRPCProperty]
        public int AscentPathIndex {
            get { return ascentType.Get (Settings) == pvg ? 2 : 0; }
            set {
                if (value != 0 && value != 2)
                    throw new ArgumentOutOfRangeException (nameof (value), "The ascent path must be 0 (classic) or 2 (PVG)");
                ascentType.Set (Settings, value == 2 ? pvg : classic);
                // MechJeb disables the other path's autopilot without removing its users
                SetEnabled (MechJeb.Module (value == 2 ? classicName : pvgName), false);
            }
        }

        /// <summary>
        /// The settings of the classic ascent profile.
        /// </summary>
        [KRPCProperty]
        public AscentClassic AscentPathClassic { get; private set; }

        /// <summary>
        /// The settings of the Primer Vector Guidance (RSS/RO) profile.
        /// </summary>
        [KRPCProperty]
        public AscentPVG AscentPathPVG { get; private set; }

        /// <summary>
        /// The altitude of the final circular orbit, in meters.
        /// </summary>
        [KRPCProperty]
        public double DesiredOrbitAltitude {
            get { return desiredOrbitAltitude.Get (Settings); }
            set { desiredOrbitAltitude.Set (Settings, value); }
        }

        /// <summary>
        /// The inclination of the final circular orbit, in degrees.
        /// </summary>
        [KRPCProperty]
        public double DesiredInclination {
            get { return desiredInclination.Get (Settings); }
            set { desiredInclination.Set (Settings, value); }
        }

        /// <summary>
        /// The thrust controller. The same as <see cref="MechJeb.ThrustController"/>.
        /// </summary>
        [KRPCProperty]
        public ThrustController ThrustController {
            get { return MechJeb.ThrustController; }
        }

        /// <summary>
        /// Whether the autopilot steers by the velocity vector rather than the position vector.
        /// </summary>
        /// <remarks>
        /// It is more accurate, though large craft may do better without it.
        /// </remarks>
        [KRPCProperty]
        public bool CorrectiveSteering {
            get { return correctiveSteering.Get (Settings); }
            set { correctiveSteering.Set (Settings, value); }
        }

        /// <summary>
        /// The gain of the corrective steering.
        /// </summary>
        /// <remarks>Requires <see cref="CorrectiveSteering"/>.</remarks>
        [KRPCProperty]
        public double CorrectiveSteeringGain {
            get { return correctiveSteeringGain.Get (Settings); }
            set { correctiveSteeringGain.Set (Settings, value); }
        }

        /// <summary>
        /// Whether the autopilot controls the roll.
        /// </summary>
        [KRPCProperty]
        public bool ForceRoll {
            get { return forceRoll.Get (Settings); }
            set { forceRoll.Set (Settings, value); }
        }

        /// <summary>
        /// The roll during the vertical climb, in degrees.
        /// </summary>
        /// <remarks>Requires <see cref="ForceRoll"/>.</remarks>
        [KRPCProperty]
        public double VerticalRoll {
            get { return verticalRoll.Get (Settings); }
            set { verticalRoll.Set (Settings, value); }
        }

        /// <summary>
        /// The roll during the turn, in degrees.
        /// </summary>
        /// <remarks>Requires <see cref="ForceRoll"/>.</remarks>
        [KRPCProperty]
        public double TurnRoll {
            get { return turnRoll.Get (Settings); }
            set { turnRoll.Set (Settings, value); }
        }

        /// <summary>
        /// Whether to deploy the solar panels when the ascent finishes.
        /// </summary>
        [KRPCProperty]
        public bool AutodeploySolarPanels {
            get { return autodeploySolarPanels.Get (Settings); }
            set { autodeploySolarPanels.Set (Settings, value); }
        }

        /// <summary>
        /// Whether to deploy the antennas when the ascent finishes.
        /// </summary>
        [KRPCProperty]
        public bool AutoDeployAntennas {
            get { return autoDeployAntennas.Get (Settings); }
            set { autoDeployAntennas.Set (Settings, value); }
        }

        /// <summary>
        /// Whether to skip the circularization burn.
        /// </summary>
        [KRPCProperty]
        public bool SkipCircularization {
            get { return skipCircularization.Get (Settings); }
            set { skipCircularization.Set (Settings, value); }
        }

        /// <summary>
        /// Whether the autopilot stages when the current stage runs out of fuel.
        /// </summary>
        /// <remarks><see cref="StagingController"/> holds the staging settings.</remarks>
        [KRPCProperty]
        public bool Autostage {
            get { return autostage.Get (Settings); }
            set { autostage.Set (Settings, value); }
        }

        /// <summary>
        /// The staging controller. The same as <see cref="MechJeb.StagingController"/>.
        /// </summary>
        [KRPCProperty]
        public StagingController StagingController {
            get { return MechJeb.StagingController; }
        }

        /// <summary>
        /// Whether to limit the angle of attack.
        /// </summary>
        [KRPCProperty]
        public bool LimitAoA {
            get { return limitAoA.Get (Settings); }
            set { limitAoA.Set (Settings, value); }
        }

        /// <summary>
        /// The largest angle of attack, in degrees.
        /// </summary>
        /// <remarks>Requires <see cref="LimitAoA"/>.</remarks>
        [KRPCProperty]
        public double MaxAoA {
            get { return maxAoA.Get (Settings); }
            set { maxAoA.Set (Settings, value); }
        }

        /// <summary>
        /// The dynamic pressure below which the angle of attack limit fades out, in pascals.
        /// </summary>
        /// <remarks>Requires <see cref="LimitAoA"/>.</remarks>
        [KRPCProperty]
        public double AoALimitFadeoutPressure {
            get { return aoaLimitFadeoutPressure.Get (Settings); }
            set { aoaLimitFadeoutPressure.Set (Settings, value); }
        }

        /// <summary>
        /// The phase angle to the target of a launch to rendezvous, in degrees.
        /// </summary>
        [KRPCProperty]
        public double LaunchPhaseAngle {
            get { return launchPhaseAngle.Get (Settings); }
            set { launchPhaseAngle.Set (Settings, value); }
        }

        /// <summary>
        /// The difference from the target's longitude of the ascending node of a launch into the
        /// target's plane, in degrees.
        /// </summary>
        [KRPCProperty]
        public double LaunchLANDifference {
            get { return launchLANDifference.Get (Settings); }
            set { launchLANDifference.Set (Settings, value); }
        }

        /// <summary>
        /// The time before a timed launch at which time warp stops, in seconds.
        /// </summary>
        [KRPCProperty]
        public int WarpCountDown {
            get { return warpCountDown.Get (Settings); }
            set { warpCountDown.Set (Settings, value); }
        }

        /// <summary>
        /// The launch mode, which gives whether the autopilot is waiting for a timed launch.
        /// </summary>
        [KRPCProperty]
        public AscentLaunchMode LaunchMode {
            get {
                var settings = Settings;
                if (!timedLaunch.Get (Instance))
                    return AscentLaunchMode.Normal;
                if (launchingToRendezvous.Get (settings))
                    return AscentLaunchMode.Rendezvous;
                if (launchingToPlane.Get (settings))
                    return AscentLaunchMode.TargetPlane;
                return AscentLaunchMode.Unknown;
            }
        }

        /// <summary>
        /// Abort a timed launch that has not started yet. Throws an exception if the launch mode
        /// is <see cref="AscentLaunchMode.Unknown"/>.
        /// </summary>
        [KRPCMethod]
        public void AbortTimedLaunch ()
        {
            if (LaunchMode == AscentLaunchMode.Unknown)
                throw new InvalidOperationException ("There is an unknown timed launch ongoing which can't be aborted");
            var settings = Settings;
            launchingToPlane.Set (settings, false);
            launchingToRendezvous.Set (settings, false);
            launchingToMatchLan.Set (settings, false);
            launchingToLan.Set (settings, false);
            timedLaunch.Set (Instance, false);
        }

        void StartCountdown (double timeOffset)
        {
            startCountdown (Instance, VesselState.Time + timeOffset);
        }

        /// <summary>
        /// Launch to rendezvous with the target. Throws an exception if there is no target, or
        /// the ascent path is PVG.
        /// </summary>
        [KRPCMethod]
        public void LaunchToRendezvous ()
        {
            if (!MechJeb.TargetController.NormalTargetExists)
                throw new InvalidOperationException ("Invalid target");
            if (AscentPathIndex == 2)
                throw new InvalidOperationException ("This action can't be performed in PVG path mode");

            AbortTimedLaunch ();
            try {
                launchingToRendezvous.Set (Settings, true);
                StartCountdown (timeToPhaseAngle (
                    LaunchPhaseAngle, FlightGlobals.ActiveVessel.mainBody, VesselState.Longitude,
                    MechJeb.TargetController.InternalTargetOrbit));
            } catch (Exception) {
                AbortTimedLaunch ();
                throw;
            }
        }

        /// <summary>
        /// Launch into the plane of the target. Throws an exception if there is no target.
        /// </summary>
        [KRPCMethod]
        public void LaunchToTargetPlane ()
        {
            if (!MechJeb.TargetController.NormalTargetExists)
                throw new InvalidOperationException ("Invalid target");

            AbortTimedLaunch ();
            try {
                var target = MechJeb.TargetController.InternalTargetOrbit;
                launchingToPlane.Set (Settings, true);
                // The sooner of the launches north and south into the plane
                var lan = target.LAN - LaunchLANDifference;
                var inclination = Math.Abs (target.inclination);
                var north = TimeToPlane (lan, inclination);
                var south = TimeToPlane (lan, -inclination);
                StartCountdown (Math.Min (north, south));
                DesiredInclination = north < south ? inclination : -inclination;
            } catch (Exception) {
                AbortTimedLaunch ();
                throw;
            }
        }

        static double TimeToPlane (double lan, double inclination)
        {
            return timeToPlane (
                FlightGlobals.ActiveVessel.mainBody.rotationPeriod, VesselState.Latitude,
                VesselState.CelestialLongitude, lan, inclination);
        }
    }

    /// <summary>
    /// The launch mode of the ascent autopilot. See <see cref="AscentAutopilot.LaunchMode"/>.
    /// </summary>
    [KRPCEnum (Service = "MechJeb")]
    public enum AscentLaunchMode
    {
        /// <summary>
        /// The autopilot is not performing a timed launch.
        /// </summary>
        Normal,

        /// <summary>
        /// A timed launch to rendezvous with the target vessel.
        /// </summary>
        Rendezvous,

        /// <summary>
        /// A timed launch into the plane of the target.
        /// </summary>
        TargetPlane,

        /// <summary>
        /// A timed launch of another kind, started from MechJeb's window.
        /// </summary>
        Unknown = 99
    }
}
