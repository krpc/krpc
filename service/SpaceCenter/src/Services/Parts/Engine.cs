using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.Engine"/>.
    /// </summary>
    /// <remarks>
    /// Provides functionality to interact with engines of various types,
    /// for example liquid fuelled gimballed engines, solid rocket boosters and jet engines.
    /// For RCS thrusters <see cref="Part.RCS"/>.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Engine : Equatable<Engine>
    {
        readonly Part part;
        readonly ModuleEngines engine;
        readonly ModuleGimbal gimbal;

        internal Engine (Part part)
        {
            this.part = part;
            engine = part.InternalPart.Module<ModuleEngines> ();
            gimbal = part.InternalPart.Module<ModuleGimbal> ();
            if (engine == null)
                throw new ArgumentException ("Part does not have a ModuleEngines PartModule");
            Thrusters = Enumerable.Range (0, engine.thrustTransforms.Count).Select (i => new Thruster (part, engine, gimbal, i)).ToList ();
        }

        /// <summary>
        /// Check the engines are equal.
        /// </summary>
        public override bool Equals (Engine obj)
        {
            return part == obj.part && engine == obj.engine && gimbal == obj.gimbal;
        }

        /// <summary>
        /// Hash the engine.
        /// </summary>
        public override int GetHashCode ()
        {
            int hash = part.GetHashCode ();
            if (engine != null)
                hash ^= engine.GetHashCode ();
            if (gimbal != null)
                hash ^= gimbal.GetHashCode ();
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
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return engine.EngineIgnited; }
            set {
                if (value)
                    engine.Activate ();
                else
                    engine.Shutdown ();
            }
        }

        /// <summary>
        /// Get the thrust of the engine with the given throttle and atmospheric conditions in Newtons
        /// </summary>
        float GetThrust (float throttle, double pressure)
        {
            pressure *= PhysicsGlobals.KpaToAtmospheres;
            return 1000f * throttle * engine.maxFuelFlow * engine.g * engine.atmosphereCurve.Evaluate ((float)pressure);
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
                return GetThrust (engine.currentThrottle, part.InternalPart.vessel.staticPressurekPa);
            }
        }

        /// <summary>
        /// The maximum available amount of thrust that can be produced by the
        /// engine, in Newtons. This takes <see cref="Engine.ThrustLimit"/> into account,
        /// and is the amount of thrust produced by the engine when activated and the
        /// main throttle is set to 100%. Returns zero if the engine does not have any fuel.
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
        /// The maximum amount of thrust that can be produced by the engine, in
        /// Newtons. This is the amount of thrust produced by the engine when
        /// activated, <see cref="Engine.ThrustLimit"/> is set to 100% and the main vessel's
        /// throttle is set to 100%.
        /// </summary>
        [KRPCProperty]
        public float MaxThrust {
            get { return GetThrust (1f, part.InternalPart.vessel.staticPressurekPa); }
        }

        /// <summary>
        /// The maximum amount of thrust that can be produced by the engine in a
        /// vacuum, in Newtons. This is the amount of thrust produced by the engine
        /// when activated, <see cref="Engine.ThrustLimit"/> is set to 100%, the main
        /// vessel's throttle is set to 100% and the engine is in a vacuum.
        /// </summary>
        [KRPCProperty]
        public float MaxVacuumThrust {
            get { return engine.maxThrust * 1000f; }
        }

        /// <summary>
        /// The thrust limiter of the engine. A value between 0 and 1. Setting this
        /// attribute may have no effect, for example the thrust limit for a solid
        /// rocket booster cannot be changed in flight.
        /// </summary>
        [KRPCProperty]
        public float ThrustLimit {
            get {
                return engine.thrustPercentage / 100f;
            }
            set {
                engine.thrustPercentage = (value * 100f).Clamp (0f, 100f);
            }
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
        public IList<Thruster> Thrusters { get; private set; }

        /// <summary>
        /// The current specific impulse of the engine, in seconds. Returns zero
        /// if the engine is not active.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get { return engine.realIsp; }
        }

        /// <summary>
        /// The vacuum specific impulse of the engine, in seconds.
        /// </summary>
        [KRPCProperty]
        public float VacuumSpecificImpulse {
            get { return engine.atmosphereCurve.Evaluate (0); }
        }

        /// <summary>
        /// The specific impulse of the engine at sea level on Kerbin, in seconds.
        /// </summary>
        [KRPCProperty]
        public float KerbinSeaLevelSpecificImpulse {
            get { return engine.atmosphereCurve.Evaluate (1); }
        }

        /// <summary>
        /// The names of resources that the engine consumes.
        /// </summary>
        [KRPCProperty]
        public IList<string> Propellants {
            get { return engine.propellants.Select (x => x.name).ToList (); }
        }

        /// <summary>
        /// The ratios of resources that the engine consumes. A dictionary mapping resource names
        /// to the ratios at which they are consumed by the engine.
        /// </summary>
        [KRPCProperty]
        public IDictionary<string, float> PropellantRatios {
            get {
                var max = engine.propellants.Max (p => p.ratio);
                return engine.propellants.ToDictionary (p => p.name, p => p.ratio / max);
            }
        }

        /// <summary>
        /// Whether the engine has run out of fuel (or flamed out).
        /// </summary>
        [KRPCProperty]
        public bool HasFuel {
            get { return !engine.flameout; }
        }

        /// <summary>
        /// The current throttle setting for the engine. A value between 0 and 1.
        /// This is not necessarily the same as the vessel's main throttle
        /// setting, as some engines take time to adjust their throttle
        /// (such as jet engines).
        /// </summary>
        [KRPCProperty]
        public float Throttle {
            get { return engine.currentThrottle; }
        }

        /// <summary>
        /// Whether the <see cref="Control.Throttle"/> affects the engine. For example,
        /// this is <c>true</c> for liquid fueled rockets, and <c>false</c> for solid rocket
        /// boosters.
        /// </summary>
        [KRPCProperty]
        public bool ThrottleLocked {
            get { return engine.throttleLocked; }
        }

        /// <summary>
        /// Whether the engine can be restarted once shutdown. If the engine cannot be shutdown,
        /// returns <c>false</c>. For example, this is <c>true</c> for liquid fueled rockets
        /// and <c>false</c> for solid rocket boosters.
        /// </summary>
        [KRPCProperty]
        public bool CanRestart {
            get { return CanShutdown && engine.allowRestart; }
        }

        /// <summary>
        /// Whether the engine can be shutdown once activated. For example, this is
        /// <c>true</c> for liquid fueled rockets and <c>false</c> for solid rocket boosters.
        /// </summary>
        [KRPCProperty]
        public bool CanShutdown {
            get { return engine.allowShutdown; }
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
    }
}
