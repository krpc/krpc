using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum PartState
    {
        Idle,
        Active,
        Dead
    }

    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Part : Equatable<Part>
    {
        readonly global::Part part;

        internal Part (global::Part part)
        {
            this.part = part;
        }

        public override bool Equals (Part obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        internal global::Part InternalPart {
            get { return part; }
        }

        [KRPCProperty]
        public string Name {
            get { return part.partInfo.name; }
        }

        [KRPCProperty]
        public string Title {
            get { return part.partInfo.title; }
        }

        [KRPCProperty]
        public double Cost {
            get { return part.partInfo.cost; }
        }

        [KRPCProperty]
        public Vessel Vessel {
            get { return new Vessel (part.vessel); }
        }

        [KRPCProperty]
        public Part Parent {
            get { return part.parent == null ? null : new Part (part.parent); }
        }

        [KRPCProperty]
        public IList<Part> Children {
            get { return part.children.Select (p => new Part (p)).ToList (); }
        }

        [KRPCProperty]
        public PartState State {
            get { return part.State.ToPartState (); }
        }

        [KRPCProperty]
        public int Stage {
            get { return part.inverseStage; }
        }

        [KRPCProperty]
        public int DecoupleStage {
            get { return part.DecoupledAt (); }
        }

        [KRPCProperty]
        public bool Massless {
            get { return part.physicalSignificance == global::Part.PhysicalSignificance.NONE; }
        }

        [KRPCProperty]
        public double Mass {
            get { return Massless ? 0f : (part.mass + part.GetResourceMass ()) * 1000f; }
        }

        [KRPCProperty]
        public double DryMass {
            get { return Massless ? 0f : part.mass * 1000f; }
        }

        [KRPCProperty]
        public double ImpactTolerance {
            get { return part.crashTolerance; }
        }

        [KRPCProperty]
        public double Temperature {
            get { return part.temperature; }
        }

        [KRPCProperty]
        public double MaxTemperature {
            get { return part.maxTemp; }
        }

        [KRPCProperty]
        public PartResources Resources {
            get { return new PartResources (part); }
        }

        [KRPCProperty]
        public IList<Module> Modules {
            get {
                IList<Module> modules = new List<Module> ();
                foreach (PartModule partModule in part.Modules)
                    modules.Add (new Module (this, partModule));
                return modules;
            }
        }

        [KRPCProperty]
        public bool IsEngine {
            get { return part.HasModule<ModuleEngines> () || part.HasModule<ModuleEnginesFX> (); }
        }

        [KRPCProperty]
        public Engine Engine {
            get { return IsEngine ? new Engine (this) : null; }
        }

        [KRPCProperty]
        public bool IsSolarPanel {
            get { return part.HasModule<ModuleDeployableSolarPanel> (); }
        }

        [KRPCProperty]
        public SolarPanel SolarPanel {
            get { return IsSolarPanel ? new SolarPanel (this) : null; }
        }

        [KRPCProperty]
        public bool IsSensor {
            get { return part.HasModule<ModuleEnviroSensor> (); }
        }

        [KRPCProperty]
        public Sensor Sensor {
            get { return IsSensor ? new Sensor (this) : null; }
        }

        [KRPCProperty]
        public bool IsDecoupler {
            get { return part.HasModule<ModuleDecouple> () || part.HasModule<ModuleAnchoredDecoupler> (); }
        }

        [KRPCProperty]
        public Decoupler Decoupler {
            get { return IsDecoupler ? new Decoupler (this) : null; }
        }

        [KRPCProperty]
        public bool IsLight {
            get { return part.HasModule<ModuleLight> (); }
        }

        [KRPCProperty]
        public Light Light {
            get { return IsLight ? new Light (this) : null; }
        }

        [KRPCProperty]
        public bool IsParachute {
            get { return part.HasModule<ModuleParachute> (); }
        }

        [KRPCProperty]
        public Parachute Parachute {
            get { return IsParachute ? new Parachute (this) : null; }
        }
    }
}
