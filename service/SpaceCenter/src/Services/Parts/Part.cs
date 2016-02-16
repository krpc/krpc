using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;
using System;
using CompoundParts;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Instances of this class represents a part. A vessel is made of multiple parts.
    /// Instances can be obtained by various methods in <see cref="Parts"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Part : Equatable<Part>
    {
        readonly global::Part part;

        internal Part (global::Part part)
        {
            this.part = part;
        }

        /// <summary>
        /// Check if the parts are equal.
        /// </summary>
        public override bool Equals (Part obj)
        {
            return part == obj.part;
        }

        /// <summary>
        /// Hash the part.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        internal global::Part InternalPart {
            get { return part; }
        }

        /// <summary>
        /// Internal name of the part, as used in
        /// <a href="http://wiki.kerbalspaceprogram.com/wiki/CFG_File_Documentation">part cfg files</a>.
        /// For example "Mark1-2Pod".
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return part.partInfo.name; }
        }

        /// <summary>
        /// Title of the part, as shown when the part is right clicked in-game. For example "Mk1-2 Command Pod".
        /// </summary>
        [KRPCProperty]
        public string Title {
            get { return part.partInfo.title; }
        }

        /// <summary>
        /// The cost of the part, in units of funds.
        /// </summary>
        [KRPCProperty]
        public double Cost {
            get { return part.partInfo.cost; }
        }

        /// <summary>
        /// The vessel that contains this part.
        /// </summary>
        [KRPCProperty]
        public Vessel Vessel {
            get { return new Vessel (part.vessel); }
        }

        /// <summary>
        /// The parts parent. Returns <c>null</c> if the part does not have a parent.
        /// This, in combination with <see cref="Part.Children"/>, can be used to traverse the vessels parts tree.
        /// </summary>
        [KRPCProperty]
        public Part Parent {
            get { return part.parent == null ? null : new Part (part.parent); }
        }

        /// <summary>
        /// The parts children. Returns an empty list if the part has no children.
        /// This, in combination with <see cref="Part.Parent"/>, can be used to traverse the vessels parts tree.
        /// </summary>
        [KRPCProperty]
        public IList<Part> Children {
            get { return part.children.Select (p => new Part (p)).ToList (); }
        }

        /// <summary>
        /// Whether the part is axially attached to its parent, i.e. on the top
        /// or bottom of its parent. If the part has no parent, returns <c>false</c>.
        /// </summary>
        [KRPCProperty]
        public bool AxiallyAttached {
            get { return part.parent == null || part.attachMode == AttachModes.STACK; }
        }

        /// <summary>
        /// Whether the part is radially attached to its parent, i.e. on the side of its parent.
        /// If the part has no parent, returns <c>false</c>.
        /// </summary>
        [KRPCProperty]
        public bool RadiallyAttached {
            get { return part.parent != null && part.attachMode == AttachModes.SRF_ATTACH; }
        }

        /// <summary>
        /// The stage in which this part will be activated. Returns -1 if the part is not activated by staging.
        /// </summary>
        [KRPCProperty]
        public int Stage {
            get { return part.hasStagingIcon ? part.inverseStage : -1; }
        }

        /// <summary>
        /// The stage in which this part will be decoupled. Returns -1 if the part is never decoupled from the vessel.
        /// </summary>
        [KRPCProperty]
        public int DecoupleStage {
            get { return part.DecoupledAt (); }
        }

        /// <summary>
        /// Whether the part is <a href="http://wiki.kerbalspaceprogram.com/wiki/Massless_part">massless</a>.
        /// </summary>
        [KRPCProperty]
        public bool Massless {
            get { return part.physicalSignificance == global::Part.PhysicalSignificance.NONE; }
        }

        /// <summary>
        /// The current mass of the part, including resources it contains, in kilograms.
        /// Returns zero if the part is massless.
        /// </summary>
        [KRPCProperty]
        public double Mass {
            get { return Massless ? 0f : (part.mass + part.GetResourceMass ()) * 1000f; }
        }

        /// <summary>
        /// The mass of the part, not including any resources it contains, in kilograms. Returns zero if the part is massless.
        /// </summary>
        [KRPCProperty]
        public double DryMass {
            get { return Massless ? 0f : part.mass * 1000f; }
        }

        /// <summary>
        /// The impact tolerance of the part, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double ImpactTolerance {
            get { return part.crashTolerance; }
        }

        /// <summary>
        /// Temperature of the part, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public double Temperature {
            get { return part.temperature; }
        }

        /// <summary>
        /// Temperature of the skin of the part, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public double SkinTemperature {
            get { return part.skinTemperature; }
        }

        /// <summary>
        /// Maximum temperature that the part can survive, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public double MaxTemperature {
            get { return part.maxTemp; }
        }

        /// <summary>
        /// Maximum temperature that the skin of the part can survive, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public double MaxSkinTemperature {
            get { return part.skinMaxTemp; }
        }

        /// <summary>
        /// Temperature of the atmosphere/vacuum surrounding the part, in Kelvin.
        /// This does not include heating from direct sunlight.
        /// </summary>
        [KRPCProperty]
        public double ExternalTemperature {
            get { throw new NotImplementedException (); }
        }

        /// <summary>
        /// How much it takes to heat up the part.
        /// </summary>
        [KRPCProperty]
        public double ThermalMass {
            get { return part.thermalMass; }
        }

        /// <summary>
        /// How much it takes to heat up the part's skin.
        /// </summary>
        [KRPCProperty]
        public double ThermalSkinMass {
            get { return part.skinThermalMass; }
        }

        /// <summary>
        /// How much it takes to heat up resources in the part.
        /// </summary>
        [KRPCProperty]
        public double ThermalResourceMass {
            get { return part.resourceThermalMass; }
        }

        /// <summary>
        /// The speed that heat is conducting into or out of the part
        /// through contact with other parts. A positive number means
        /// the part is gaining heat energy, negative means it is
        /// losing heat energy.
        /// </summary>
        [KRPCProperty]
        public double ThermalConductionFlux {
            get { return part.thermalConductionFlux; }
        }

        /// <summary>
        /// The speed that heat is convecting into or out of the part
        /// from the surrounding atmosphere. A positive number means
        /// the part is gaining heat energy, negative means it is losing
        /// heat energy.
        /// </summary>
        [KRPCProperty]
        public double ThermalConvectionFlux {
            get { return part.thermalConvectionFlux; }
        }

        /// <summary>
        /// The speed that heat is radiating into or out of the part
        /// from the surrounding vacuum. A positive number means the part
        /// is gaining heat energy, negative means it is losing heat energy.
        /// </summary>
        [KRPCProperty]
        public double ThermalRadiationFlux {
            get { return part.thermalRadiationFlux; }
        }

        /// <summary>
        /// The speed that heat is generated by the part. For example,
        /// engines generate heat by burning fuel. A positive number means
        /// the part is gaining heat energy, negative means it is losing
        /// heat energy.
        /// </summary>
        [KRPCProperty]
        public double ThermalInternalFlux {
            get { return part.thermalInternalFlux; }
        }

        /// <summary>
        /// The speed that heat is conducting between the part's skin and its internals.
        /// </summary>
        [KRPCProperty]
        public double ThermalSkinToInternalFlux {
            get { return part.skinToInternalFlux; }
        }

        /// <summary>
        /// A <see cref="Resources"/> object for the part.
        /// </summary>
        [KRPCProperty]
        public Resources Resources {
            get { return new Resources (part); }
        }

        /// <summary>
        /// Whether this part is crossfeed capable.
        /// </summary>
        [KRPCProperty]
        public bool Crossfeed {
            get { return part.fuelCrossFeed; }
        }

        /// <summary>
        /// Whether this part is a fuel line.
        /// </summary>
        [KRPCProperty]
        public bool IsFuelLine {
            get { return part.HasModule<CModuleFuelLine> (); }
        }

        /// <summary>
        /// The parts that are connected to this part via fuel lines, where the direction of the fuel line is into this part.
        /// </summary>
        [KRPCProperty]
        public IList<Part> FuelLinesFrom {
            get {
                if (IsFuelLine)
                    throw new ArgumentException ("Part is a fuel line");
                return part.fuelLookupTargets.Select (x => new Part (x.parent)).ToList ();
            }
        }

        /// <summary>
        /// The parts that are connected to this part via fuel lines, where the direction of the fuel line is out of this part.
        /// </summary>
        [KRPCProperty]
        public IList<Part> FuelLinesTo {
            get {
                if (IsFuelLine)
                    throw new ArgumentException ("Part is a fuel line");
                var result = new List<global::Part> ();
                foreach (var otherPart in part.vessel.parts) {
                    foreach (var target in otherPart.fuelLookupTargets.Select (x => x.parent)) {
                        if (target == part)
                            result.Add (otherPart);
                    }
                }
                return result.Select (x => new Part (x)).ToList ();
            }
        }

        /// <summary>
        /// The modules for this part.
        /// </summary>
        [KRPCProperty]
        public IList<Module> Modules {
            get {
                IList<Module> modules = new List<Module> ();
                foreach (PartModule partModule in part.Modules)
                    modules.Add (new Module (this, partModule));
                return modules;
            }
        }

        internal bool IsDecoupler {
            get { return part.HasModule<ModuleDecouple> () || part.HasModule<ModuleAnchoredDecoupler> (); }
        }

        internal bool IsDockingPort {
            get { return part.HasModule<ModuleDockingNode> (); }
        }

        internal bool IsResourceConverter {
            get { return part.HasModule<ModuleResourceConverter> (); }
        }

        internal bool IsResourceHarvester {
            get { return part.HasModule<ModuleResourceHarvester> (); }
        }

        internal bool IsEngine {
            get { return part.HasModule<ModuleEngines> () || part.HasModule<ModuleEnginesFX> (); }
        }

        internal bool IsLandingGear {
            get { return part.HasModule<ModuleLandingGear> (); }
        }

        internal bool IsLandingLeg {
            get { return part.HasModule<ModuleLandingLeg> (); }
        }

        internal bool IsLaunchClamp {
            get { return part.HasModule<global::LaunchClamp> (); }
        }

        internal bool IsLight {
            get { return part.HasModule<ModuleLight> (); }
        }

        internal bool IsParachute {
            get { return part.HasModule<ModuleParachute> (); }
        }

        internal bool IsRadiator {
            get { return part.HasModule<ModuleDeployableRadiator> (); }
        }

        internal bool IsReactionWheel {
            get { return part.HasModule<ModuleReactionWheel> (); }
        }

        internal bool IsSensor {
            get { return part.HasModule<ModuleEnviroSensor> (); }
        }

        internal bool IsSolarPanel {
            get { return part.HasModule<ModuleDeployableSolarPanel> (); }
        }

        /// <summary>
        /// A <see cref="Decoupler"/> if the part is a decoupler, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Decoupler Decoupler {
            get { return IsDecoupler ? new Decoupler (this) : null; }
        }

        /// <summary>
        /// A <see cref="DockingPort"/> if the part is a docking port, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public DockingPort DockingPort {
            get { return IsDockingPort ? new DockingPort (this) : null; }
        }

        /// <summary>
        /// A <see cref="ResourceConverter"/> if the part is a resource converter, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public ResourceConverter ResourceConverter {
            get { return IsResourceConverter ? new ResourceConverter (this) : null; }
        }

        /// <summary>
        /// A <see cref="ResourceHarvester"/> if the part is a resource harvester, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public ResourceHarvester ResourceHarvester {
            get { return IsResourceHarvester ? new ResourceHarvester (this) : null; }
        }

        /// <summary>
        /// An <see cref="Engine"/> if the part is an engine, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Engine Engine {
            get { return IsEngine ? new Engine (this) : null; }
        }

        /// <summary>
        /// A <see cref="LandingGear"/> if the part is a landing gear , otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public LandingGear LandingGear {
            get { return IsLandingGear ? new LandingGear (this) : null; }
        }

        /// <summary>
        /// A <see cref="LandingLeg"/> if the part is a landing leg, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public LandingLeg LandingLeg {
            get { return IsLandingLeg ? new LandingLeg (this) : null; }
        }

        /// <summary>
        /// A <see cref="LaunchClamp"/> if the part is a launch clamp, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public LaunchClamp LaunchClamp {
            get { return IsLaunchClamp ? new LaunchClamp (this) : null; }
        }

        /// <summary>
        /// A <see cref="Light"/> if the part is a light, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Light Light {
            get { return IsLight ? new Light (this) : null; }
        }

        /// <summary>
        /// A <see cref="Parachute"/> if the part is a parachute, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Parachute Parachute {
            get { return IsParachute ? new Parachute (this) : null; }
        }

        /// <summary>
        /// A <see cref="Radiator"/> if the part is a radiator, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Radiator Radiator {
            get { return IsRadiator ? new Radiator (this) : null; }
        }

        /// <summary>
        /// A <see cref="ReactionWheel"/> if the part is a reaction wheel, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public ReactionWheel ReactionWheel {
            get { return IsReactionWheel ? new ReactionWheel (this) : null; }
        }

        /// <summary>
        /// A <see cref="Sensor"/> if the part is a sensor, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Sensor Sensor {
            get { return IsSensor ? new Sensor (this) : null; }
        }

        /// <summary>
        /// A <see cref="SolarPanel"/> if the part is a solar panel, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public SolarPanel SolarPanel {
            get { return IsSolarPanel ? new SolarPanel (this) : null; }
        }

        /// <summary>
        /// The position of the part in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (part.transform.position).ToTuple ();
        }

        /// <summary>
        /// The direction of the part in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (part.transform.up).ToTuple ();
        }

        /// <summary>
        /// The velocity of the part in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Velocity (ReferenceFrame referenceFrame)
        {
            return referenceFrame.VelocityFromWorldSpace (part.transform.position, part.orbit.GetVel ()).ToTuple ();
        }

        /// <summary>
        /// The rotation of the part in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            return referenceFrame.RotationToWorldSpace (part.transform.rotation).ToTuple ();
        }

        /// <summary>
        /// The reference frame that is fixed relative to this part.
        /// <list type="bullet">
        /// <item><description>The origin is at the position of the part.</description></item>
        /// <item><description>The axes rotate with the part.</description></item>
        /// <item><description>The x, y and z axis directions depend on the design of the part.</description></item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// For docking port parts, this reference frame is not necessarily equivalent to the reference frame
        /// for the docking port, returned by <see cref="DockingPort.ReferenceFrame"/>.
        /// </remarks>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.Object (part); }
        }
    }
}
