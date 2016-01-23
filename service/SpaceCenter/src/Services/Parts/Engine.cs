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
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Engine : Equatable<Engine>
    {
        readonly Part part;
        readonly ModuleEngines engine;
        readonly ModuleEnginesFX engineFx;
        readonly ModuleGimbal gimbal;

        internal Engine (Part part)
        {
            this.part = part;
            engine = part.InternalPart.Module<ModuleEngines> ();
            engineFx = part.InternalPart.Module<ModuleEnginesFX> ();
            gimbal = part.InternalPart.Module<ModuleGimbal> ();
            if (engine == null && engineFx == null)
                throw new ArgumentException ("Part does not have a ModuleEngines or ModuleEnginexFX PartModule");
        }

        public override bool Equals (Engine obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
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
            get { return engine != null ? engine.EngineIgnited : engineFx.EngineIgnited; }
            set {
                if (value) {
                    if (engine != null)
                        engine.Activate ();
                    else
                        engineFx.Activate ();
                } else {
                    if (engine != null)
                        engine.Shutdown ();
                    else
                        engineFx.Shutdown ();
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
            else
                return 1000f * throttle * engineFx.maxFuelFlow * engineFx.g * engineFx.atmosphereCurve.Evaluate ((float)pressure);
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
                var throttle = engine != null ? engine.currentThrottle : engineFx.currentThrottle;
                return GetThrust (throttle, part.InternalPart.vessel.staticPressurekPa);
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
        /// Gets the maximum amount of thrust that can be produced by the engine, in
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
            get { return (engine != null ? engine.maxThrust : engineFx.maxThrust) * 1000f; }
        }

        /// <summary>
        /// The thrust limiter of the engine. A value between 0 and 1. Setting this
        /// attribute may have no effect, for example the thrust limit for a solid
        /// rocket booster cannot be changed in flight.
        /// </summary>
        [KRPCProperty]
        public float ThrustLimit {
            get {
                return (engine != null ? engine.thrustPercentage : engineFx.thrustPercentage) / 100f;
            }
            set {
                value = (value * 100f).Clamp (0f, 100f);
                if (engine != null)
                    engine.thrustPercentage = value;
                else
                    engineFx.thrustPercentage = value;
            }
        }

        /// <summary>
        /// The current specific impulse of the engine, in seconds. Returns zero
        /// if the engine is not active.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get { return engine != null ? engine.realIsp : engineFx.realIsp; }
        }

        /// <summary>
        /// The vacuum specific impulse of the engine, in seconds.
        /// </summary>
        [KRPCProperty]
        public float VacuumSpecificImpulse {
            get { return (engine != null ? engine.atmosphereCurve : engineFx.atmosphereCurve).Evaluate (0); }
        }

        /// <summary>
        /// The specific impulse of the engine at sea level on Kerbin, in seconds.
        /// </summary>
        [KRPCProperty]
        public float KerbinSeaLevelSpecificImpulse {
            get { return (engine != null ? engine.atmosphereCurve : engineFx.atmosphereCurve).Evaluate (1); }
        }

        /// <summary>
        /// The names of resources that the engine consumes.
        /// </summary>
        [KRPCProperty]
        public IList<string> Propellants {
            get {
                var propellants = new List<string> ();
                foreach (var propellant in (engine != null ? engine.propellants : engineFx.propellants))
                    propellants.Add (propellant.name);
                return propellants;
            }
        }

        /// <summary>
        /// The ratios of resources that the engine consumes. A dictionary mapping resource names
        /// to the ratios at which they are consumed by the engine.
        /// </summary>
        [KRPCProperty]
        public IDictionary<string, float> PropellantRatios {
            get {
                var max = (engine != null ? engine.propellants : engineFx.propellants).Max (p => p.ratio);
                var ratios = new Dictionary<string, float> ();
                foreach (var propellant in (engine != null ? engine.propellants : engineFx.propellants))
                    ratios [propellant.name] = propellant.ratio / max;
                return ratios;
            }
        }

        /// <summary>
        /// Whether the engine has run out of fuel (or flamed out).
        /// </summary>
        [KRPCProperty]
        public bool HasFuel {
            get { return !(engine != null ? engine.flameout : engineFx.flameout); }
        }

        /// <summary>
        /// The current throttle setting for the engine. A value between 0 and 1.
        /// This is not necessarily the same as the vessel's main throttle
        /// setting, as some engines take time to adjust their throttle
        /// (such as jet engines).
        /// </summary>
        [KRPCProperty]
        public float Throttle {
            get { return engine != null ? engine.currentThrottle : engineFx.currentThrottle; }
        }

        /// <summary>
        /// Whether the <see cref="Control.Throttle"/> affects the engine. For example,
        /// this is <c>true</c> for liquid fueled rockets, and <c>false</c> for solid rocket
        /// boosters.
        /// </summary>
        [KRPCProperty]
        public bool ThrottleLocked {
            get { return engine != null ? engine.throttleLocked : engineFx.throttleLocked; }
        }

        /// <summary>
        /// Whether the engine can be restarted once shutdown. If the engine cannot be shutdown,
        /// returns <c>false</c>. For example, this is <c>true</c> for liquid fueled rockets
        /// and <c>false</c> for solid rocket boosters.
        /// </summary>
        [KRPCProperty]
        public bool CanRestart {
            get { return CanShutdown && (engine != null ? engine.allowRestart : engineFx.allowRestart); }
        }

        /// <summary>
        /// Gets whether the engine can be shutdown once activated. For example, this is
        /// <c>true</c> for liquid fueled rockets and <c>false</c> for solid rocket boosters.
        /// </summary>
        [KRPCProperty]
        public bool CanShutdown {
            get { return engine != null ? engine.allowShutdown : engineFx.allowShutdown; }
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
    }
}
