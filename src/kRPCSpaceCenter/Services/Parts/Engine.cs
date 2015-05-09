using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
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

        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

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

        [KRPCProperty]
        public float Thrust {
            get {
                var throttle = engine != null ? engine.currentThrottle : engineFx.currentThrottle;
                return GetThrust (throttle, part.InternalPart.vessel.staticPressurekPa);
            }
        }

        [KRPCProperty]
        public float AvailableThrust {
            get { return GetThrust (ThrustLimit, part.InternalPart.vessel.staticPressurekPa); }
        }

        [KRPCProperty]
        public float MaxThrust {
            get { return GetThrust (1f, part.InternalPart.vessel.staticPressurekPa); }
        }

        [KRPCProperty]
        public float MaxVacuumThrust {
            get { return (engine != null ? engine.maxThrust : engineFx.maxThrust) * 1000f; }
        }

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

        [KRPCProperty]
        public float SpecificImpulse {
            get { return engine != null ? engine.realIsp : engineFx.realIsp; }
        }

        [KRPCProperty]
        public float VacuumSpecificImpulse {
            get { return (engine != null ? engine.atmosphereCurve : engineFx.atmosphereCurve).Evaluate (0); }
        }

        [KRPCProperty]
        public float KerbinSeaLevelSpecificImpulse {
            get { return (engine != null ? engine.atmosphereCurve : engineFx.atmosphereCurve).Evaluate (1); }
        }

        [KRPCProperty]
        public IList<string> Propellants {
            get {
                var propellants = new List<string> ();
                foreach (var propellant in (engine != null ? engine.propellants : engineFx.propellants))
                    propellants.Add (propellant.name);
                return propellants;
            }
        }

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

        [KRPCProperty]
        public bool HasFuel {
            get { return !(engine != null ? engine.flameout : engineFx.flameout); }
        }

        [KRPCProperty]
        public float Throttle {
            get { return engine != null ? engine.currentThrottle : engineFx.currentThrottle; }
        }

        [KRPCProperty]
        public bool ThrottleLocked {
            get { return engine != null ? engine.throttleLocked : engineFx.throttleLocked; }
        }

        [KRPCProperty]
        public bool CanRestart {
            get { return CanShutdown && (engine != null ? engine.allowRestart : engineFx.allowRestart); }
        }

        [KRPCProperty]
        public bool CanShutdown {
            get { return engine != null ? engine.allowShutdown : engineFx.allowShutdown; }
        }

        [KRPCProperty]
        public bool Gimballed {
            get { return gimbal != null; }
        }

        [KRPCProperty]
        public float GimbalRange {
            get { return gimbal != null && !gimbal.gimbalLock ? gimbal.gimbalRange : 0f; }
        }

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
