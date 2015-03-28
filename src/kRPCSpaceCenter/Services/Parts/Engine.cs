using System;
using System.Collections.Generic;
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
        public bool Activated {
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
        public float Thrust {
            get { return engine != null ? engine.finalThrust * 1000f : engineFx.finalThrust * 1000f; }
        }

        [KRPCProperty]
        public float MaxThrust {
            get { return engine != null ? engine.maxThrust * 1000f : engineFx.maxThrust * 1000f; }
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
        public bool HasFuel {
            get { return !(engine != null ? engine.flameout : engineFx.flameout); }
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
                else if (value)
                    gimbal.LockGimbal ();
                else
                    gimbal.FreeGimbal ();
            }
        }
    }
}
