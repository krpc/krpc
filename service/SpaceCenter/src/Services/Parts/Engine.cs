using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An engine, including ones of various types.
    /// For example liquid fuelled gimballed engines, solid rocket boosters and jet engines.
    /// Obtained by calling <see cref="Part.Engine"/>.
    /// </summary>
    /// <remarks>
    /// For RCS thrusters <see cref="Part.RCS"/>.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class Engine : Equatable<Engine>
    {
        readonly IList<ModuleEngines> engines;
        readonly MultiModeEngine multiModeEngine;
        readonly ModuleGimbal gimbal;

        internal static bool Is (Part part)
        {
            return Is (part.InternalPart);
        }

        internal static bool Is (global::Part part)
        {
            return part.HasModule<ModuleEngines> ();
        }

        internal Engine (Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            engines = internalPart.Modules.OfType<ModuleEngines> ().ToList ();
            multiModeEngine = internalPart.Module<MultiModeEngine> ();
            gimbal = internalPart.Module<ModuleGimbal> ();
            if (engines.Count == 0)
                throw new ArgumentException ("Part is not an engine");
        }

        Engine (ModuleEngines engine)
        {
            Part = new Part (engine.part);
            engines = new List<ModuleEngines> ();
            engines.Add (engine);
            gimbal = Part.InternalPart.Module<ModuleGimbal> ();
            if (engine == null)
                throw new ArgumentException ("Part does not have a ModuleEngines PartModule");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Engine other)
        {
            return
            !ReferenceEquals (other, null) &&
            Part == other.Part &&
            engines.SequenceEqual (other.engines) &&
            (multiModeEngine == other.multiModeEngine || multiModeEngine.Equals (other.multiModeEngine)) &&
            (gimbal == other.gimbal || gimbal.Equals (other.gimbal));
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            int hash = Part.GetHashCode ();
            hash ^= engines.GetHashCode ();
            foreach (var engine in engines)
                hash ^= engine.GetHashCode ();
            if (multiModeEngine != null)
                hash ^= multiModeEngine.GetHashCode ();
            if (gimbal != null)
                hash ^= gimbal.GetHashCode ();
            return hash;
        }

        /// <summary>
        /// Get the currently active ModuleEngines part module. For a single-mode engine, this is just the
        /// ModulesEngine for the part. For multi-mode engines, this is the ModulesEngine for the current mode.
        /// </summary>
        ModuleEngines CurrentEngine {
            get { return engines [(multiModeEngine == null || multiModeEngine.runningPrimary) ? 0 : 1]; }
        }

        /// <summary>
        /// The part object for this engine.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the engine is active. Setting this attribute may have no effect,
        /// depending on <see cref="CanShutdown"/> and <see cref="CanRestart"/>.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return CurrentEngine.EngineIgnited; }
            set {
                var engine = CurrentEngine;
                if (value)
                    engine.Activate ();
                else
                    engine.Shutdown ();
            }
        }

        /// <summary>
        /// Get the thrust of the engine in Newtons, with the given throttle percentage and atmospheric pressure in atmospheres
        /// </summary>
        float GetThrust (float throttle, double pressure)
        {
            var engine = CurrentEngine;

            // Compute fuel flow multiplier
            float flowMultiplier = 1;
            if (engine.atmChangeFlow)
                flowMultiplier = (float)(engine.part.atmDensity / 1.225d);
            if (engine.useAtmCurve && engine.atmCurve != null)
                flowMultiplier = engine.atmCurve.Evaluate (flowMultiplier);

            // Compute velocity multiplier
            float velocityMultiplier = 1;
            if (engine.useVelCurve && engine.velCurve != null)
                velocityMultiplier = velocityMultiplier * engine.velCurve.Evaluate ((float)engine.vessel.mach);

            // Get specific impulse at the given pressure
            var specificImpulse = engine.atmosphereCurve.Evaluate ((float)pressure);

            // Compute thrust
            return 1000f * Mathf.Lerp (engine.minFuelFlow, engine.maxFuelFlow, throttle) * flowMultiplier * specificImpulse * engine.g * velocityMultiplier;
        }

        /// <summary>
        /// The current amount of thrust being produced by the engine, in Newtons.
        /// </summary>
        [KRPCProperty]
        public float Thrust {
            get {
                if (!Active || !HasFuel)
                    return 0f;
                return CurrentEngine.finalThrust * 1000f;
            }
        }

        /// <summary>
        /// The amount of thrust, in Newtons, that would be produced by the engine
        /// when activated and with its throttle set to 100%.
        /// Returns zero if the engine does not have any fuel.
        /// Takes the engine's current <see cref="ThrustLimit"/> and atmospheric conditions into account.
        /// </summary>
        [KRPCProperty]
        public float AvailableThrust {
            get {
                if (!HasFuel)
                    return 0f;
                return GetThrust (ThrustLimit, Part.InternalPart.staticPressureAtm);
            }
        }

        /// <summary>
        /// The amount of thrust, in Newtons, that would be produced by the engine
        /// when activated and fueled, with its throttle and throttle limiter set to 100%.
        /// </summary>
        [KRPCProperty]
        public float MaxThrust {
            get { return GetThrust (1f, Part.InternalPart.staticPressureAtm); }
        }

        /// <summary>
        /// The maximum amount of thrust that can be produced by the engine in a
        /// vacuum, in Newtons. This is the amount of thrust produced by the engine
        /// when activated, <see cref="ThrustLimit"/> is set to 100%, the main
        /// vessel's throttle is set to 100% and the engine is in a vacuum.
        /// </summary>
        [KRPCProperty]
        public float MaxVacuumThrust {
            get { return CurrentEngine.maxThrust * 1000f; }
        }

        /// <summary>
        /// The thrust limiter of the engine. A value between 0 and 1. Setting this
        /// attribute may have no effect, for example the thrust limit for a solid
        /// rocket booster cannot be changed in flight.
        /// </summary>
        [KRPCProperty]
        public float ThrustLimit {
            get { return CurrentEngine.thrustPercentage / 100f; }
            set { CurrentEngine.thrustPercentage = (value * 100f).Clamp (0f, 100f); }
        }

        /// <summary>
        /// The components of the engine that generate thrust.
        /// </summary>
        /// <remarks>
        /// For example, this corresponds to the rocket nozzel on a solid rocket booster,
        /// or the individual nozzels on a RAPIER engine.
        /// The overall thrust produced by the engine, as reported by <see cref="AvailableThrust"/>,
        /// <see cref="MaxThrust"/> and others, is the sum of the thrust generated by each thruster.
        /// </remarks>
        [KRPCProperty]
        public IList<Thruster> Thrusters {
            get {
                var engine = CurrentEngine;
                return Enumerable.Range (0, engine.thrustTransforms.Count).Select (i => new Thruster (Part, engine, gimbal, i)).ToList ();
            }
        }

        /// <summary>
        /// The current specific impulse of the engine, in seconds. Returns zero
        /// if the engine is not active.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get { return CurrentEngine.realIsp; }
        }

        /// <summary>
        /// The vacuum specific impulse of the engine, in seconds.
        /// </summary>
        [KRPCProperty]
        public float VacuumSpecificImpulse {
            get { return CurrentEngine.atmosphereCurve.Evaluate (0); }
        }

        /// <summary>
        /// The specific impulse of the engine at sea level on Kerbin, in seconds.
        /// </summary>
        [KRPCProperty]
        public float KerbinSeaLevelSpecificImpulse {
            get { return CurrentEngine.atmosphereCurve.Evaluate (1); }
        }

        /// <summary>
        /// The names of the propellants that the engine consumes.
        /// </summary>
        [KRPCProperty]
        public IList<string> PropellantNames {
            get { return CurrentEngine.propellants.Select (x => x.name).ToList (); }
        }

        /// <summary>
        /// The propellants that the engine consumes.
        /// </summary>
        [KRPCProperty]
        public IList<Propellant> Propellants {
            get {
                return CurrentEngine.propellants.Select (propellant => new Propellant (propellant, CurrentEngine.part)).ToList ();
            }
        }

        /// <summary>
        /// The ratio of resources that the engine consumes. A dictionary mapping resource names
        /// to the ratio at which they are consumed by the engine.
        /// </summary>
        /// <remarks>
        /// For example, if the ratios are 0.6 for LiquidFuel and 0.4 for Oxidizer, then for every 0.6 units of
        /// LiquidFuel that the engine burns, it will burn 0.4 units of Oxidizer.
        /// </remarks>
        [KRPCProperty]
        public IDictionary<string, float> PropellantRatios {
            get {
                var engine = CurrentEngine;
                var max = engine.propellants.Max (p => p.ratio);
                return engine.propellants.ToDictionary (p => p.name, p => p.ratio / max);
            }
        }

        /// <summary>
        /// Whether the engine has any fuel available.
        /// </summary>
        /// <remarks>
        /// The engine must be activated for this property to update correctly.
        /// </remarks>
        // FIXME: should not have to enable the RCS thruster for this to update
        [KRPCProperty]
        public bool HasFuel {
            get {
                var engine = CurrentEngine;
                if (engine.flameout)
                    return false;
                foreach (var propellant in engine.propellants)
                    if (propellant.isDeprived)
                        return false;
                return true;
            }
        }

        /// <summary>
        /// The current throttle setting for the engine. A value between 0 and 1.
        /// This is not necessarily the same as the vessel's main throttle
        /// setting, as some engines take time to adjust their throttle
        /// (such as jet engines).
        /// </summary>
        [KRPCProperty]
        public float Throttle {
            get { return CurrentEngine.currentThrottle; }
        }

        /// <summary>
        /// Whether the <see cref="Control.Throttle"/> affects the engine. For example,
        /// this is <c>true</c> for liquid fueled rockets, and <c>false</c> for solid rocket
        /// boosters.
        /// </summary>
        [KRPCProperty]
        public bool ThrottleLocked {
            get { return CurrentEngine.throttleLocked; }
        }

        /// <summary>
        /// Whether the engine can be restarted once shutdown. If the engine cannot be shutdown,
        /// returns <c>false</c>. For example, this is <c>true</c> for liquid fueled rockets
        /// and <c>false</c> for solid rocket boosters.
        /// </summary>
        [KRPCProperty]
        public bool CanRestart {
            get { return CanShutdown && CurrentEngine.allowRestart; }
        }

        /// <summary>
        /// Whether the engine can be shutdown once activated. For example, this is
        /// <c>true</c> for liquid fueled rockets and <c>false</c> for solid rocket boosters.
        /// </summary>
        [KRPCProperty]
        public bool CanShutdown {
            get { return CurrentEngine.allowShutdown; }
        }

        void CheckMultiMode ()
        {
            if (multiModeEngine == null)
                throw new InvalidOperationException ("The engine only has a single mode");
        }

        void CheckHasMode (string id)
        {
            CheckMultiMode ();
            if (multiModeEngine.primaryEngineID != id && multiModeEngine.secondaryEngineID != id)
                throw new InvalidOperationException ("The engine does not have the given mode");
        }

        /// <summary>
        /// Whether the engine has multiple modes of operation.
        /// </summary>
        [KRPCProperty]
        public bool HasModes {
            get { return multiModeEngine != null; }
        }

        /// <summary>
        /// The name of the current engine mode.
        /// </summary>
        [KRPCProperty]
        public string Mode {
            get {
                CheckMultiMode ();
                return multiModeEngine.mode;
            }
            set {
                CheckHasMode (value);
                if (value == multiModeEngine.mode)
                    return;
                multiModeEngine.Invoke ("ModeEvent", 0);
            }
        }

        /// <summary>
        /// The available modes for the engine.
        /// A dictionary mapping mode names to <see cref="Engine" /> objects.
        /// </summary>
        [KRPCProperty]
        public IDictionary<string,Engine> Modes {
            get {
                CheckMultiMode ();
                var result = new Dictionary<string,Engine> ();
                result [multiModeEngine.primaryEngineID] = new Engine (engines [0]);
                result [multiModeEngine.secondaryEngineID] = new Engine (engines [1]);
                return result;
            }
        }

        /// <summary>
        /// Toggle the current engine mode.
        /// </summary>
        [KRPCMethod]
        public void ToggleMode ()
        {
            CheckMultiMode ();
            multiModeEngine.Invoke ("ModeEvent", 0);
        }

        /// <summary>
        /// Whether the engine will automatically switch modes.
        /// </summary>
        [KRPCProperty]
        public bool AutoModeSwitch {
            get {
                CheckMultiMode ();
                return multiModeEngine.autoSwitch;
            }
            set {
                CheckMultiMode ();
                if (value == multiModeEngine.autoSwitch)
                    return;
                if (value)
                    multiModeEngine.Invoke ("EnableAutoSwitch", 0);
                else
                    multiModeEngine.Invoke ("DisableAutoSwitch", 0);
            }
        }

        void CheckGimballed ()
        {
            if (gimbal == null)
                throw new InvalidOperationException ("Engine is not gimballed");
        }

        /// <summary>
        /// Whether the engine is gimballed.
        /// </summary>
        [KRPCProperty]
        public bool Gimballed {
            get { return gimbal != null; }
        }

        /// <summary>
        /// The range over which the gimbal can move, in degrees.
        /// Returns 0 if the engine is not gimballed.
        /// </summary>
        [KRPCProperty]
        public float GimbalRange {
            get { return gimbal != null && !gimbal.gimbalLock ? gimbal.gimbalRange : 0f; }
        }

        /// <summary>
        /// Whether the engines gimbal is locked in place. Setting this attribute has
        /// no effect if the engine is not gimballed.
        /// </summary>
        [KRPCProperty]
        public bool GimbalLocked {
            get {
                CheckGimballed ();
                return gimbal.gimbalLock;
            }
            set {
                CheckGimballed ();
                if (value && !gimbal.gimbalLock) {
                    gimbal.LockAction (new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate));
                } else if (!value && gimbal.gimbalLock) {
                    gimbal.FreeAction (new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate));
                }
            }
        }

        /// <summary>
        /// The gimbal limiter of the engine. A value between 0 and 1.
        /// Returns 0 if the gimbal is locked.
        /// </summary>
        [KRPCProperty]
        public float GimbalLimit {
            get {
                CheckGimballed ();
                return GimbalLocked ? 0f : gimbal.gimbalLimiter / 100f;
            }
            set {
                CheckGimballed ();
                gimbal.gimbalLimiter = (value * 100f).Clamp (0f, 100f);
            }
        }

        /// <summary>
        /// The available torque in the pitch, roll and yaw axes of the vessel, in Newton meters.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// Returns zero if the engine is inactive, or not gimballed.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        public Tuple<Tuple3, Tuple3> AvailableTorque {
            get { return AvailableTorqueVectors.ToTuple (); }
        }

        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        internal Tuple<Vector3d, Vector3d> AvailableTorqueVectors {
            get {
                if (!Active || !Gimballed)
                    return ITorqueProviderExtensions.zero;
                var torque = gimbal.GetPotentialTorque ();
                // Note: GetPotentialTorque returns negative torques with incorrect sign
                return new Tuple<Vector3d,Vector3d> (torque.Item1, -torque.Item2);
            }
        }
    }
}
