using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.Engine"/> or <see cref="RCS.Thrusters" />.
    /// Provides functionality to interact with engines of various types,
    /// including liquid fuelled gimballed engines, solid rocket boosters and
    /// individual RCS thrusters.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Engine : Equatable<Engine>
    {
        readonly Part part;

        readonly ModuleEngines engine;
        readonly ModuleEnginesFX engineFx;
        readonly ModuleGimbal gimbal;

        readonly ModuleRCS rcs;
        readonly Transform rcsThrustTransform;

        /// <summary>
        /// Construct an engine from a part with ModulesEngine or ModulesEngineFX
        /// </summary>
        internal Engine (Part part)
        {
            this.part = part;
            engine = part.InternalPart.Module<ModuleEngines> ();
            engineFx = part.InternalPart.Module<ModuleEnginesFX> ();
            gimbal = part.InternalPart.Module<ModuleGimbal> ();
            rcs = null;
            rcsThrustTransform = null;
            if (engine == null && engineFx == null)
                throw new ArgumentException ("Part does not have a ModuleEngines or ModuleEnginexFX");
        }

        /// <summary>
        /// Construct an engine from a part with ModuleRCS and the thrust transform for the specific thruster
        /// </summary>
        internal Engine (Part part, Transform rcsThrustTransform)
        {
            this.part = part;
            engine = null;
            engineFx = null;
            gimbal = null;
            rcs = part.InternalPart.Module<ModuleRCS> ();
            this.rcsThrustTransform = rcsThrustTransform;
            if (rcs == null)
                throw new ArgumentException ("Part does not have a ModuleRCS");
        }

        /// <summary>
        /// Check the engines are equal.
        /// </summary>
        public override bool Equals (Engine obj)
        {
            return
                part == obj.part &&
            engine == obj.engine &&
            engineFx == obj.engineFx &&
            gimbal == obj.gimbal &&
            rcs == obj.rcs &&
            rcsThrustTransform == obj.rcsThrustTransform;
        }

        /// <summary>
        /// Hash the engine.
        /// </summary>
        public override int GetHashCode ()
        {
            int hash = part.GetHashCode ();
            if (engine != null)
                hash ^= engine.GetHashCode ();
            if (engineFx != null)
                hash ^= engineFx.GetHashCode ();
            if (gimbal != null)
                hash ^= gimbal.GetHashCode ();
            if (rcs != null)
                hash ^= rcs.GetHashCode ();
            if (rcsThrustTransform != null)
                hash ^= rcsThrustTransform.GetHashCode ();
            return hash;
        }

        /// <summary>
        /// The part object for this engine.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Whether the engine is active. Setting this attribute may have no effect,
        /// depending on <see cref="Engine.CanShutdown"/> and <see cref="Engine.CanRestart"/>.
        /// Note that enable or disabling an RCS thruster will enable/disable ALL of the thrusters on the part.
        /// When enabling RCS thrusters, the RCS action group also needs to be enabled.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get {
                if (engine != null)
                    return engine.EngineIgnited;
                else if (engineFx != null)
                    return engineFx.EngineIgnited;
                else {
                    var p = part.InternalPart;
                    return p.vessel.ActionGroups [KSPActionGroup.RCS] &&
                    !p.ShieldedFromAirstream && rcs.rcsEnabled && rcs.isEnabled && !rcs.isJustForShow;
                }
            }
            set {
                if (value) {
                    if (engine != null)
                        engine.Activate ();
                    else if (engineFx != null)
                        engineFx.Activate ();
                    else
                        rcs.rcsEnabled = true;
                } else {
                    if (engine != null)
                        engine.Shutdown ();
                    else if (engineFx != null)
                        engineFx.Shutdown ();
                    else
                        rcs.rcsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Get the thrust of the engine with the given throttle and atmospheric conditions in Newtons
        /// </summary>
        float GetThrust (float throttle, double pressure)
        {
            pressure *= PhysicsGlobals.KpaToAtmospheres;
            if (engine != null)
                return 1000f * throttle * engine.maxFuelFlow * engine.g * engine.atmosphereCurve.Evaluate ((float)pressure);
            else if (engineFx != null)
                return 1000f * throttle * engineFx.maxFuelFlow * engineFx.g * engineFx.atmosphereCurve.Evaluate ((float)pressure);
            else
                throw new NotImplementedException ();
        }

        /// <summary>
        /// The current amount of thrust being produced by the engine, in
        /// Newtons. Returns zero if the engine is not active or if it has no fuel.
        /// </summary>
        [KRPCProperty]
        public float Thrust {
            get {
                if (!Active || !HasFuel)
                    return 0f;
                if (rcs != null)
                    throw new NotImplementedException ();
                var throttle = engine != null ? engine.currentThrottle : engineFx.currentThrottle;
                return GetThrust (throttle, part.InternalPart.vessel.staticPressurekPa);
            }
        }

        /// <summary>
        /// The maximum available amount of thrust that can be produced by the
        /// engine, in Newtons. This takes <see cref="Engine.ThrustLimit"/> into account,
        /// and is the amount of thrust produced by the engine when activated and the
        /// throttle is set to 100%. Returns zero if the engine does not have any fuel.
        /// </summary>
        [KRPCProperty]
        public float AvailableThrust {
            get {
                if (!HasFuel)
                    return 0f;
                return GetThrust (ThrustLimit, part.InternalPart.vessel.staticPressurekPa);
            }
        }

        /// <summary>
        /// Gets the maximum amount of thrust that can be produced by the engine, in
        /// Newtons. This is the amount of thrust produced by the engine when
        /// activated, <see cref="Engine.ThrustLimit"/> is set to 100% and the throttle
        /// is set to 100%.
        /// </summary>
        [KRPCProperty]
        public float MaxThrust {
            get { return GetThrust (1f, part.InternalPart.vessel.staticPressurekPa); }
        }

        /// <summary>
        /// The maximum amount of thrust that can be produced by the engine in a
        /// vacuum, in Newtons. This is the amount of thrust produced by the engine
        /// when activated, <see cref="Engine.ThrustLimit"/> is set to 100%, the
        /// throttle is set to 100% and the engine is in a vacuum.
        /// </summary>
        [KRPCProperty]
        public float MaxVacuumThrust {
            get {
                if (engine != null)
                    return engine.maxThrust * 1000f;
                else if (engineFx != null)
                    return engineFx.maxThrust * 1000f;
                else
                    throw new NotImplementedException ();
            }
        }

        /// <summary>
        /// The thrust limiter of the engine. A value between 0 and 1. Setting this
        /// attribute may have no effect, for example the thrust limit for a solid
        /// rocket booster cannot be changed in flight.
        /// </summary>
        [KRPCProperty]
        public float ThrustLimit {
            get {
                if (engine != null)
                    return engine.thrustPercentage / 100f;
                else if (engineFx != null)
                    return engineFx.thrustPercentage / 100f;
                else
                    throw new NotImplementedException ();
            }
            set {
                value = (value * 100f).Clamp (0f, 100f);
                if (engine != null)
                    engine.thrustPercentage = value;
                else if (engineFx != null)
                    engineFx.thrustPercentage = value;
                else
                    throw new NotImplementedException ();
            }
        }

        /// <summary>
        /// The position at which the engine generates thrust, in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame">Reference frame of the resulting position.</param>
        [KRPCMethod]
        public Tuple3 ThrustPosition (ReferenceFrame referenceFrame)
        {
            throw new NotImplementedException ();
        }

        /// <summary>
        /// The direction of the force generated by the engine, in the given reference frame.
        /// This is opposite to the direction in which the engine expels propellant.
        /// For gimballed engines, this takes into account the current rotation of the gimbal.
        /// See <see cref="InitialThrustDirection" /> for the thrust direction for the initial position
        /// of the engine, ignoring the current rotation of the gimbal.
        /// </summary>
        /// <param name="referenceFrame">Reference frame of the resulting direction vector.</param>
        [KRPCMethod]
        public Tuple3 ThrustDirection (ReferenceFrame referenceFrame)
        {
            throw new NotImplementedException ();
        }

        /// <summary>
        /// The direction of the force generated by the engine, in the given reference frame.
        /// This is opposite to the direction in which the engine expels propellant.
        /// For gimballed engines, this ignores the current rotation of the gimbal.
        /// See <see cref="ThrustDirection" /> for the thrust direction including the
        /// current rotation of the gimbal.
        /// </summary>
        /// <param name="referenceFrame">Reference frame of the resulting direction vector.</param>
        [KRPCMethod]
        public Tuple3 InitialThrustDirection (ReferenceFrame referenceFrame)
        {
            throw new NotImplementedException ();
        }

        /// <summary>
        /// The reference frame that is fixed relative to the engine, and orientated with
        /// its initial thrust direction (<see cref="InitialThrustDirection"/>).
        /// Note that for gimballed engines, this reference frame is orientated with
        /// the initial rotation of the engine, ignoring the current state of the gimbal.
        /// <list type="bullet">
        /// <item><description>
        /// The origin is at the position of thrust (<see cref="ThrustPosition"/>).
        /// </description></item>
        /// <item><description>
        /// The axes rotate with the engines initial thrust direction.
        /// This is the direction in which the engine expels propellant, ignoring any gimballing.
        /// </description></item>
        /// <item><description>The y-axis points along the thrust direction.</description></item>
        /// <item><description>The x-axis and z-axis are perpendicular to the thrust direction.</description></item>
        /// </list>
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ThrustReferenceFrame {
            get {
                if (engine != null)
                    return ReferenceFrame.Thrust (part.InternalPart, engine);
                else if (engineFx != null)
                    return ReferenceFrame.Thrust (part.InternalPart, engineFx);
                else
                    return ReferenceFrame.Thrust (part.InternalPart, rcs, rcsThrustTransform);
            }
        }

        /// <summary>
        /// The current specific impulse of the engine, in seconds. Returns zero
        /// if the engine is not active.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get {
                if (engine != null)
                    return engine.realIsp;
                else if (engineFx != null)
                    return engineFx.realIsp;
                else
                    throw new NotImplementedException ();
            }
        }

        /// <summary>
        /// The vacuum specific impulse of the engine, in seconds.
        /// </summary>
        [KRPCProperty]
        public float VacuumSpecificImpulse {
            get {
                if (engine != null)
                    return engine.atmosphereCurve.Evaluate (0);
                else if (engineFx != null)
                    return engineFx.atmosphereCurve.Evaluate (0);
                else
                    throw new NotImplementedException ();
            }
        }

        /// <summary>
        /// The specific impulse of the engine at sea level on Kerbin, in seconds.
        /// </summary>
        [KRPCProperty]
        public float KerbinSeaLevelSpecificImpulse {
            get {
                if (engine != null)
                    return engine.atmosphereCurve.Evaluate (1);
                else if (engineFx != null)
                    return engineFx.atmosphereCurve.Evaluate (1);
                else
                    throw new NotImplementedException ();
            }
        }

        /// <summary>
        /// The names of resources that the engine consumes.
        /// </summary>
        [KRPCProperty]
        public IList<string> Propellants {
            get {
                if (rcs != null)
                    throw new NotImplementedException ();
                var propellants = (engine != null ? engine.propellants : engineFx.propellants);
                return propellants.Select (x => x.name).ToList ();
            }
        }

        /// <summary>
        /// The ratios of resources that the engine consumes. A dictionary mapping resource names
        /// to the ratios at which they are consumed by the engine.
        /// </summary>
        [KRPCProperty]
        public IDictionary<string, float> PropellantRatios {
            get {
                if (rcs != null)
                    throw new NotImplementedException ();
                var propellants = (engine != null ? engine.propellants : engineFx.propellants);
                var max = propellants.Max (p => p.ratio);
                var ratios = new Dictionary<string, float> ();
                foreach (var propellant in propellants)
                    ratios [propellant.name] = propellant.ratio / max;
                return ratios;
            }
        }

        /// <summary>
        /// Whether the engine has run out of fuel (or flamed out).
        /// </summary>
        [KRPCProperty]
        public bool HasFuel {
            get {
                if (rcs != null)
                    throw new NotImplementedException ();
                return !(engine != null ? engine.flameout : engineFx.flameout);
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
            get {
                if (engine != null)
                    return engine.currentThrottle;
                else if (engineFx != null)
                    return engineFx.currentThrottle;
                else
                    throw new NotImplementedException ();
            }
        }

        /// <summary>
        /// Whether the <see cref="Control.Throttle"/> affects the engine. For example,
        /// this is <c>true</c> for liquid fueled rockets, and <c>false</c> for solid rocket
        /// boosters.
        /// </summary>
        [KRPCProperty]
        public bool ThrottleLocked {
            get {
                if (engine != null)
                    return engine.throttleLocked;
                else if (engineFx != null)
                    return engineFx.throttleLocked;
                else
                    throw new NotImplementedException ();
            }
        }

        /// <summary>
        /// Whether the engine can be restarted once shutdown. If the engine cannot be shutdown,
        /// returns <c>false</c>. For example, this is <c>true</c> for liquid fueled rockets
        /// and <c>false</c> for solid rocket boosters.
        /// </summary>
        [KRPCProperty]
        public bool CanRestart {
            get {
                return
                    rcs != null ||
                (engine != null && engine.allowRestart) ||
                (engineFx != null && engineFx.allowRestart);
            }
        }

        /// <summary>
        /// Gets whether the engine can be shutdown once activated. For example, this is
        /// <c>true</c> for liquid fueled rockets and <c>false</c> for solid rocket boosters.
        /// </summary>
        [KRPCProperty]
        public bool CanShutdown {
            get {
                return
                    rcs != null ||
                (engine != null && engine.allowShutdown) ||
                (engineFx != null && engineFx.allowShutdown);
            }
        }

        /// <summary>
        /// Whether the engine nozzle is gimballed, i.e. can provide a turning force.
        /// </summary>
        [KRPCProperty]
        public bool Gimballed {
            get { return gimbal != null; }
        }

        /// <summary>
        /// The range over which the gimbal can move, in degrees.
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
            get { return gimbal != null && gimbal.gimbalLock; }
            set {
                if (gimbal == null)
                    throw new ArgumentException ("Engine is not gimballed");
                if (value && !GimbalLocked) {
                    gimbal.LockAction (new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate));
                } else if (!value && GimbalLocked) {
                    gimbal.FreeAction (new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate));
                }
            }
        }

        /// <summary>
        /// The gimbal limiter of the engine. A value between 0 and 1. Returns 0 if the
        /// gimbal is locked or the engine is not gimballed. Setting this attribute has
        /// no effect if the engine is not gimballed.
        /// </summary>
        [KRPCProperty]
        public float GimbalLimit {
            get {
                if (gimbal == null)
                    return 1f;
                else if (GimbalLocked)
                    return 0f;
                else
                    return gimbal.gimbalLimiter / 100f;
            }
            set {
                if (gimbal == null)
                    return;
                value = (value * 100f).Clamp (0f, 100f);
                gimbal.gimbalLimiter = value;
            }
        }

        /// <summary>
        /// The current rotation of the gimbal, in the given reference frame.
        /// This is a quarternion describing the rotation engine nozzel away from it's initial position.
        /// To get the gimbal rotation relative to the initial direction of the engine,
        /// use <see cref="Engine.ThrustReferenceFrame" />.
        /// </summary>
        /// <param name="referenceFrame">Reference frame of the resulting direction vector.</param>
        [KRPCMethod]
        public Tuple4 GimbalRotation (ReferenceFrame referenceFrame)
        {
            throw new NotImplementedException ();
        }
    }
}
