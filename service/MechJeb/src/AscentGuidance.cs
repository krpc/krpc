using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Ascent Guidance window, which flies the vessel to orbit.
    /// </summary>
    /// <remarks>
    /// See the <a href="https://github.com/MuMech/MechJeb2/wiki/Ascent-Guidance">MechJeb wiki</a>
    /// for how to set up this autopilot.
    /// </remarks>
    [KRPCClass (Service = "MechJeb")]
    public sealed class AscentGuidance : KRPCComputerModule
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
        static Field<object, double> desiredApoapsis;
        static Field<object, bool> attachAltFlag;
        static Field<object, double> desiredAttachAlt;
        static Field<object, double> desiredAttachAltFixed;
        static Func<object, int> optimizeStage;
        static Field<object, bool> limitQaEnabled;
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
            desiredApoapsis = Bindings.EditableDouble (settings, "DesiredApoapsis");
            attachAltFlag = settings.Field<object, bool> ("AttachAltFlag");
            desiredAttachAlt = Bindings.EditableDouble (settings, "DesiredAttachAlt");
            desiredAttachAltFixed = Bindings.EditableDouble (settings, "DesiredAttachAltFixed");
            optimizeStage = settings.Getter<object, int> ("OptimizeStage");
            limitQaEnabled = settings.Field<object, bool> ("LimitQaEnabled");
            launchPhaseAngle = Bindings.EditableDouble (settings, "LaunchPhaseAngle");
            launchLANDifference = Bindings.EditableDouble (settings, "LaunchLANDifference");
            warpCountDown = Bindings.EditableInt (settings, "WarpCountDown");
            launchingToPlane = settings.Field<object, bool> ("LaunchingToPlane");
            launchingToRendezvous = settings.Field<object, bool> ("LaunchingToRendezvous");
            launchingToMatchLan = settings.Field<object, bool> ("LaunchingToMatchLan");
            launchingToLan = settings.Field<object, bool> ("LaunchingToLan");
        }

        internal AscentGuidance () : base (settingsName)
        {
            Settings = new AscentSettings ();
            PVGSettings = new PVGSettings ();
            ClassicPath = new ClassicPath ();
        }

        /// <summary>
        /// MechJeb's ascent settings, which hold the settings of every ascent path.
        /// </summary>
        internal static object SettingsModule {
            get { return MechJeb.Module (settingsName); }
        }

        /// <summary>
        /// The autopilot of the selected ascent path.
        /// </summary>
        internal override object Instance {
            get { return autopilot (SettingsModule); }
        }

        /// <summary>
        /// Engage the autopilot of the selected ascent path, or disengage the autopilots of
        /// both paths.
        /// </summary>
        internal override void SetEnabled (bool value)
        {
            if (value) {
                // Select the path's angle of attack limiter, as MechJeb's ascent window does
                var settings = SettingsModule;
                limitQaEnabled.Set (settings, ascentType.Get (settings) == pvg);
                SetEnabled (Instance, true);
            } else {
                // The path may have changed since the autopilot was enabled
                SetEnabled (MechJeb.Module (classicName), false);
                SetEnabled (MechJeb.Module (pvgName), false);
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
        /// The ascent path.
        /// </summary>
        [KRPCProperty]
        public AscentType AscentType {
            get { return ascentType.Get (SettingsModule) == pvg ? AscentType.PVG : AscentType.Classic; }
            set {
                var settings = SettingsModule;
                var isPVG = value == AscentType.PVG;
                ascentType.Set (settings, isPVG ? pvg : classic);
                limitQaEnabled.Set (settings, isPVG);
                // MechJeb disables the other path's autopilot without removing its users
                SetEnabled (MechJeb.Module (isPVG ? classicName : pvgName), false);
            }
        }

        /// <summary>
        /// The Ascent Settings window.
        /// </summary>
        [KRPCProperty]
        public AscentSettings Settings { get; private set; }

        /// <summary>
        /// The PVG Settings window, with the settings of the <see cref="AscentType.PVG"/> path.
        /// </summary>
        [KRPCProperty]
        public PVGSettings PVGSettings { get; private set; }

        /// <summary>
        /// The Ascent Path Editor window, with the settings of the
        /// <see cref="AscentType.Classic"/> path.
        /// </summary>
        [KRPCProperty]
        public ClassicPath ClassicPath { get; private set; }

        /// <summary>
        /// The altitude of the final circular orbit, in meters.
        /// </summary>
        [KRPCProperty]
        public double DesiredOrbitAltitude {
            get { return desiredOrbitAltitude.Get (SettingsModule); }
            set { desiredOrbitAltitude.Set (SettingsModule, value); }
        }

        /// <summary>
        /// The inclination of the final circular orbit, in degrees.
        /// </summary>
        [KRPCProperty]
        public double DesiredInclination {
            get { return desiredInclination.Get (SettingsModule); }
            set { desiredInclination.Set (SettingsModule, value); }
        }

        /// <summary>
        /// The target apoapsis, in meters.
        /// </summary>
        /// <remarks>Applies on the <see cref="AscentType.PVG"/> path.</remarks>
        [KRPCProperty]
        public double DesiredApoapsis {
            get { return desiredApoapsis.Get (SettingsModule); }
            set { desiredApoapsis.Set (SettingsModule, value); }
        }

        /// <summary>
        /// Whether the orbit is attached at <see cref="DesiredAttachAlt"/>.
        /// </summary>
        /// <remarks>
        /// Applies on the <see cref="AscentType.PVG"/> path. MechJeb reads this setting only
        /// when its PVG settings window has an early shutoff stage. Without one, MechJeb always
        /// attaches the orbit.
        /// </remarks>
        [KRPCProperty]
        public bool AttachAltFlag {
            get { return attachAltFlag.Get (SettingsModule); }
            set { attachAltFlag.Set (SettingsModule, value); }
        }

        /// <summary>
        /// The altitude at which the orbit is attached, in meters.
        /// </summary>
        /// <remarks>
        /// Applies on the <see cref="AscentType.PVG"/> path. Requires <see cref="AttachAltFlag"/>,
        /// unless MechJeb's PVG settings window has no early shutoff stage.
        /// </remarks>
        [KRPCProperty]
        public double DesiredAttachAlt {
            get { return AttachAltitude.Get (SettingsModule); }
            set { AttachAltitude.Set (SettingsModule, value); }
        }

        // MechJeb keeps a separate attach altitude for an ascent with no early shutoff stage
        static Field<object, double> AttachAltitude {
            get { return optimizeStage (SettingsModule) < 0 ? desiredAttachAltFixed : desiredAttachAlt; }
        }

        /// <summary>
        /// The phase angle to the target of a launch to rendezvous, in degrees.
        /// </summary>
        [KRPCProperty]
        public double LaunchPhaseAngle {
            get { return launchPhaseAngle.Get (SettingsModule); }
            set { launchPhaseAngle.Set (SettingsModule, value); }
        }

        /// <summary>
        /// The difference from the target's longitude of the ascending node of a launch into the
        /// target's plane, in degrees.
        /// </summary>
        [KRPCProperty]
        public double LaunchLANDifference {
            get { return launchLANDifference.Get (SettingsModule); }
            set { launchLANDifference.Set (SettingsModule, value); }
        }

        /// <summary>
        /// The time before a timed launch at which time warp stops, in seconds.
        /// </summary>
        [KRPCProperty]
        public int WarpCountDown {
            get { return warpCountDown.Get (SettingsModule); }
            set { warpCountDown.Set (SettingsModule, value); }
        }

        /// <summary>
        /// The launch mode, which gives whether the autopilot is waiting for a timed launch.
        /// </summary>
        [KRPCProperty]
        public AscentLaunchMode LaunchMode {
            get {
                var settings = SettingsModule;
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
            var settings = SettingsModule;
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
            if (AscentType == AscentType.PVG)
                throw new InvalidOperationException ("This action can't be performed in PVG path mode");

            AbortTimedLaunch ();
            try {
                launchingToRendezvous.Set (SettingsModule, true);
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
                launchingToPlane.Set (SettingsModule, true);
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
    /// The launch mode of the ascent autopilot. See <see cref="AscentGuidance.LaunchMode"/>.
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

    /// <summary>
    /// The ascent path. See <see cref="AscentGuidance.AscentType"/>.
    /// </summary>
    [KRPCEnum (Service = "MechJeb")]
    public enum AscentType
    {
        /// <summary>
        /// The classic ascent path. See <see cref="AscentGuidance.ClassicPath"/>.
        /// </summary>
        Classic,

        /// <summary>
        /// Primer Vector Guidance, for RSS/RO. See <see cref="AscentGuidance.PVGSettings"/>.
        /// </summary>
        PVG
    }
}
