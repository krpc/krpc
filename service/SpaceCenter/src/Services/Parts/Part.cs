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
        readonly uint partFlightId;

        internal Part (global::Part part)
        {
            this.partFlightId = part.flightID;
        }

        /// <summary>
        /// Check if the parts are equal.
        /// </summary>
        public override bool Equals (Part obj)
        {
            return partFlightId == obj.partFlightId;
        }

        /// <summary>
        /// Hash the part.
        /// </summary>
        public override int GetHashCode ()
        {
            return partFlightId.GetHashCode ();
        }

        /// <summary>
        /// The KSP part.
        /// </summary>
        public global::Part InternalPart {
            get { return FlightGlobals.FindPartByID (partFlightId); }
        }

        /// <summary>
        /// Internal name of the part, as used in
        /// <a href="http://wiki.kerbalspaceprogram.com/wiki/CFG_File_Documentation">part cfg files</a>.
        /// For example "Mark1-2Pod".
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalPart.partInfo.name; }
        }

        /// <summary>
        /// Title of the part, as shown when the part is right clicked in-game. For example "Mk1-2 Command Pod".
        /// </summary>
        [KRPCProperty]
        public string Title {
            get { return InternalPart.partInfo.title; }
        }

        /// <summary>
        /// The cost of the part, in units of funds.
        /// </summary>
        [KRPCProperty]
        public double Cost {
            get { return InternalPart.partInfo.cost; }
        }

        /// <summary>
        /// The vessel that contains this part.
        /// </summary>
        [KRPCProperty]
        public Vessel Vessel {
            get { return new Vessel (InternalPart.vessel); }
        }

        /// <summary>
        /// The parts parent. Returns <c>null</c> if the part does not have a parent.
        /// This, in combination with <see cref="Part.Children"/>, can be used to traverse the vessels parts tree.
        /// </summary>
        [KRPCProperty]
        public Part Parent {
            get { return InternalPart.parent == null ? null : new Part (InternalPart.parent); }
        }

        /// <summary>
        /// The parts children. Returns an empty list if the part has no children.
        /// This, in combination with <see cref="Part.Parent"/>, can be used to traverse the vessels parts tree.
        /// </summary>
        [KRPCProperty]
        public IList<Part> Children {
            get { return InternalPart.children.Select (p => new Part (p)).ToList (); }
        }

        /// <summary>
        /// Whether the part is axially attached to its parent, i.e. on the top
        /// or bottom of its parent. If the part has no parent, returns <c>false</c>.
        /// </summary>
        [KRPCProperty]
        public bool AxiallyAttached {
            get { return InternalPart.parent == null || InternalPart.attachMode == AttachModes.STACK; }
        }

        /// <summary>
        /// Whether the part is radially attached to its parent, i.e. on the side of its parent.
        /// If the part has no parent, returns <c>false</c>.
        /// </summary>
        [KRPCProperty]
        public bool RadiallyAttached {
            get { return InternalPart.parent != null && InternalPart.attachMode == AttachModes.SRF_ATTACH; }
        }

        /// <summary>
        /// The stage in which this part will be activated. Returns -1 if the part is not activated by staging.
        /// </summary>
        [KRPCProperty]
        public int Stage {
            get { return InternalPart.hasStagingIcon ? InternalPart.inverseStage : -1; }
        }

        /// <summary>
        /// The stage in which this part will be decoupled. Returns -1 if the part is never decoupled from the vessel.
        /// </summary>
        [KRPCProperty]
        public int DecoupleStage {
            get { return InternalPart.DecoupledAt (); }
        }

        /// <summary>
        /// Whether the part is <a href="http://wiki.kerbalspaceprogram.com/wiki/Massless_part">massless</a>.
        /// </summary>
        [KRPCProperty]
        public bool Massless {
            get { return InternalPart.physicalSignificance == global::Part.PhysicalSignificance.NONE; }
        }

        /// <summary>
        /// The current mass of the part, including resources it contains, in kilograms.
        /// Returns zero if the part is massless.
        /// </summary>
        [KRPCProperty]
        public double Mass {
            get { return Massless ? 0f : (InternalPart.mass + InternalPart.GetResourceMass ()) * 1000f; }
        }

        /// <summary>
        /// The mass of the part, not including any resources it contains, in kilograms. Returns zero if the part is massless.
        /// </summary>
        [KRPCProperty]
        public double DryMass {
            get { return Massless ? 0f : InternalPart.mass * 1000f; }
        }

        /// <summary>
        /// Whether the part is shielded from the exterior of the vessel, for example by a fairing.
        /// </summary>
        [KRPCProperty]
        public bool Shielded {
            get { return InternalPart.ShieldedFromAirstream; }
        }

        /// <summary>
        /// The dynamic pressure acting on the part, in Pascals.
        /// </summary>
        [KRPCProperty]
        public float DynamicPressure {
            get { return (float)InternalPart.dynamicPressurekPa * 1000f; }
        }

        /// <summary>
        /// The impact tolerance of the part, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double ImpactTolerance {
            get { return InternalPart.crashTolerance; }
        }

        /// <summary>
        /// Temperature of the part, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public double Temperature {
            get { return InternalPart.temperature; }
        }

        /// <summary>
        /// Temperature of the skin of the part, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public double SkinTemperature {
            get { return InternalPart.skinTemperature; }
        }

        /// <summary>
        /// Maximum temperature that the part can survive, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public double MaxTemperature {
            get { return InternalPart.maxTemp; }
        }

        /// <summary>
        /// Maximum temperature that the skin of the part can survive, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public double MaxSkinTemperature {
            get { return InternalPart.skinMaxTemp; }
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
            get { return InternalPart.thermalMass; }
        }

        /// <summary>
        /// How much it takes to heat up the part's skin.
        /// </summary>
        [KRPCProperty]
        public double ThermalSkinMass {
            get { return InternalPart.skinThermalMass; }
        }

        /// <summary>
        /// How much it takes to heat up resources in the part.
        /// </summary>
        [KRPCProperty]
        public double ThermalResourceMass {
            get { return InternalPart.resourceThermalMass; }
        }

        /// <summary>
        /// The speed that heat is conducting into or out of the part
        /// through contact with other parts. A positive number means
        /// the part is gaining heat energy, negative means it is
        /// losing heat energy.
        /// </summary>
        [KRPCProperty]
        public double ThermalConductionFlux {
            get { return InternalPart.thermalConductionFlux; }
        }

        /// <summary>
        /// The speed that heat is convecting into or out of the part
        /// from the surrounding atmosphere. A positive number means
        /// the part is gaining heat energy, negative means it is losing
        /// heat energy.
        /// </summary>
        [KRPCProperty]
        public double ThermalConvectionFlux {
            get { return InternalPart.thermalConvectionFlux; }
        }

        /// <summary>
        /// The speed that heat is radiating into or out of the part
        /// from the surrounding vacuum. A positive number means the part
        /// is gaining heat energy, negative means it is losing heat energy.
        /// </summary>
        [KRPCProperty]
        public double ThermalRadiationFlux {
            get { return InternalPart.thermalRadiationFlux; }
        }

        /// <summary>
        /// The speed that heat is generated by the part. For example,
        /// engines generate heat by burning fuel. A positive number means
        /// the part is gaining heat energy, negative means it is losing
        /// heat energy.
        /// </summary>
        [KRPCProperty]
        public double ThermalInternalFlux {
            get { return InternalPart.thermalInternalFlux; }
        }

        /// <summary>
        /// The speed that heat is conducting between the part's skin and its internals.
        /// </summary>
        [KRPCProperty]
        public double ThermalSkinToInternalFlux {
            get { return InternalPart.skinToInternalFlux; }
        }

        /// <summary>
        /// A <see cref="Resources"/> object for the part.
        /// </summary>
        [KRPCProperty]
        public Resources Resources {
            get { return new Resources (InternalPart); }
        }

        /// <summary>
        /// Whether this part is crossfeed capable.
        /// </summary>
        [KRPCProperty]
        public bool Crossfeed {
            get { return InternalPart.fuelCrossFeed; }
        }

        /// <summary>
        /// Whether this part is a fuel line.
        /// </summary>
        [KRPCProperty]
        public bool IsFuelLine {
            get { return InternalPart.HasModule<CModuleFuelLine> (); }
        }

        /// <summary>
        /// The parts that are connected to this part via fuel lines, where the direction of the fuel line is into this part.
        /// </summary>
        [KRPCProperty]
        public IList<Part> FuelLinesFrom {
            get {
                if (IsFuelLine)
                    throw new ArgumentException ("Part is a fuel line");
                return InternalPart.fuelLookupTargets.Select (x => new Part (x.parent)).ToList ();
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
                foreach (var otherPart in InternalPart.vessel.parts) {
                    foreach (var target in otherPart.fuelLookupTargets.Select (x => x.parent)) {
                        if (target.flightID == partFlightId)
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
                foreach (PartModule partModule in InternalPart.Modules)
                    modules.Add (new Module (this, partModule));
                return modules;
            }
        }

        internal bool IsControlSurface {
            get { return InternalPart.HasModule<ModuleControlSurface> (); }
        }

        internal bool IsDecoupler {
            get { return InternalPart.HasModule<ModuleDecouple> () || InternalPart.HasModule<ModuleAnchoredDecoupler> (); }
        }

        internal bool IsDockingPort {
            get { return InternalPart.HasModule<ModuleDockingNode> (); }
        }

        internal bool IsResourceConverter {
            get { return InternalPart.HasModule<ModuleResourceConverter> (); }
        }

        internal bool IsResourceHarvester {
            get { return InternalPart.HasModule<ModuleResourceHarvester> (); }
        }

        internal bool IsEngine {
            get { return InternalPart.HasModule<ModuleEngines> () || InternalPart.HasModule<ModuleEnginesFX> (); }
        }

        internal bool IsLandingGear {
            get { return InternalPart.HasModule<ModuleLandingGear> (); }
        }

        internal bool IsLandingLeg {
            get { return InternalPart.HasModule<ModuleLandingLeg> (); }
        }

        internal bool IsLaunchClamp {
            get { return InternalPart.HasModule<global::LaunchClamp> (); }
        }

        internal bool IsLight {
            get { return InternalPart.HasModule<ModuleLight> (); }
        }

        internal bool IsParachute {
            get { return InternalPart.HasModule<ModuleParachute> (); }
        }

        internal bool IsRadiator {
            get { return InternalPart.HasModule<ModuleDeployableRadiator> (); }
        }

        internal bool IsRCS {
            get { return InternalPart.HasModule<ModuleRCS> (); }
        }

        internal bool IsReactionWheel {
            get { return InternalPart.HasModule<ModuleReactionWheel> (); }
        }

        internal bool IsSensor {
            get { return InternalPart.HasModule<ModuleEnviroSensor> (); }
        }

        internal bool IsSolarPanel {
            get { return InternalPart.HasModule<ModuleDeployableSolarPanel> (); }
        }

        /// <summary>
        /// A <see cref="ControlSurface"/> if the part is an aerodynamic control surface, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public ControlSurface ControlSurface {
            get { return IsControlSurface ? new ControlSurface (this) : null; }
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
        /// <remarks>
        /// This includes any part that generates thrust. This covers many different types of engine,
        /// including liquid fuel rockets, solid rocket boosters and jet engines.
        /// For RCS thrusters see <see cref="RCS"/>.
        /// </remarks>
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
        /// A <see cref="RCS"/> if the part is an RCS block/thruster, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public RCS RCS {
            get { return IsRCS ? new RCS (this) : null; }
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
        /// <remarks>
        /// This is a fixed position in the part, defined by the parts model.
        /// It s not necessarily the same as the parts center of mass.
        /// Use <see cref="CenterOfMass"/> to get the parts center of mass.
        /// </remarks>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (InternalPart.transform.position).ToTuple ();
        }

        /// <summary>
        /// The position of the parts center of mass in the given reference frame.
        /// If the part is physicsless, this is equivalent to <see cref="Position"/>.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 CenterOfMass (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (InternalPart.CenterOfMass ()).ToTuple ();
        }

        /// <summary>
        /// The direction of the part in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (InternalPart.transform.up).ToTuple ();
        }

        /// <summary>
        /// The velocity of the part in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Velocity (ReferenceFrame referenceFrame)
        {
            return referenceFrame.VelocityFromWorldSpace (InternalPart.transform.position, InternalPart.orbit.GetVel ()).ToTuple ();
        }

        /// <summary>
        /// The rotation of the part in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            return referenceFrame.RotationToWorldSpace (InternalPart.transform.rotation).ToTuple ();
        }

        /// <summary>
        /// The reference frame that is fixed relative to this part, and centered on a fixed position within the part, defined by the parts model.
        /// <list type="bullet">
        /// <item><description>The origin is at the position of the part, as returned by <see cref="Position"/>.</description></item>
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
            get { return ReferenceFrame.Object (InternalPart); }
        }

        /// <summary>
        /// The reference frame that is fixed relative to this part, and centered on its center of mass.
        /// <list type="bullet">
        /// <item><description>The origin is at the center of mass of the part, as returned by <see cref="CenterOfMass"/>.</description></item>
        /// <item><description>The axes rotate with the part.</description></item>
        /// <item><description>The x, y and z axis directions depend on the design of the part.</description></item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// For docking port parts, this reference frame is not necessarily equivalent to the reference frame
        /// for the docking port, returned by <see cref="DockingPort.ReferenceFrame"/>.
        /// </remarks>
        [KRPCProperty]
        public ReferenceFrame CenterOfMassReferenceFrame {
            get { return ReferenceFrame.ObjectCenterOfMass (InternalPart); }
        }
    }
}
