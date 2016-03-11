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
        /// A measure of how much energy it takes to increase the internal temperature of the part, in Joules per Kelvin.
        /// </summary>
        [KRPCProperty]
        public float ThermalMass {
            get { return (float)InternalPart.thermalMass / 1000f; }
        }

        /// <summary>
        /// A measure of how much energy it takes to increase the skin temperature of the part, in Joules per Kelvin.
        /// </summary>
        [KRPCProperty]
        public float ThermalSkinMass {
            get { return (float)InternalPart.skinThermalMass / 1000f; }
        }

        /// <summary>
        /// A measure of how much energy it takes to increase the temperature of the resources contained in the part, in Joules per Kelvin.
        /// </summary>
        [KRPCProperty]
        public float ThermalResourceMass {
            get { return (float)InternalPart.resourceThermalMass / 1000f; }
        }

        /// <summary>
        /// The rate at which heat energy is begin generated by the part.
        /// For example, some engines generate heat by combusting fuel.
        /// Measured in energy per unit time, or power, in Watts.
        /// A positive value means the part is gaining heat energy, and negative means it is losing heat energy.
        /// </summary>
        [KRPCProperty]
        public float ThermalInternalFlux {
            get { return (float)InternalPart.thermalInternalFluxPrevious / 1000f; }
        }

        /// <summary>
        /// The rate at which heat energy is conducting into or out of the part via contact with other parts.
        /// Measured in energy per unit time, or power, in Watts.
        /// A positive value means the part is gaining heat energy, and negative means it is losing heat energy.
        /// </summary>
        [KRPCProperty]
        public float ThermalConductionFlux {
            get { return (float)InternalPart.thermalConductionFlux / 1000f; }
        }

        /// <summary>
        /// The rate at which heat energy is convecting into or out of the part from the surrounding atmosphere.
        /// Measured in energy per unit time, or power, in Watts.
        /// A positive value means the part is gaining heat energy, and negative means it is losing heat energy.
        /// </summary>
        [KRPCProperty]
        public float ThermalConvectionFlux {
            get { return (float)InternalPart.thermalConvectionFlux / 1000f; }
        }

        /// <summary>
        /// The rate at which heat energy is radiating into or out of the part from the surrounding environment.
        /// Measured in energy per unit time, or power, in Watts.
        /// A positive value means the part is gaining heat energy, and negative means it is losing heat energy.
        /// </summary>
        [KRPCProperty]
        public float ThermalRadiationFlux {
            get { return (float)InternalPart.thermalRadiationFlux / 1000f; }
        }

        /// <summary>
        /// The rate at which heat energy is transferring between the part's skin and its internals.
        /// Measured in energy per unit time, or power, in Watts.
        /// A positive value means the part's internals are gaining heat energy,
        /// and negative means its skin is gaining heat energy.
        /// </summary>
        [KRPCProperty]
        public float ThermalSkinToInternalFlux {
            get { return (float)InternalPart.skinToInternalFlux / 1000f; }
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

        /// <summary>
        /// A <see cref="Decoupler"/> if the part is a decoupler, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Decoupler Decoupler {
            get { return Decoupler.Is (this) ? new Decoupler (this) : null; }
        }

        /// <summary>
        /// A <see cref="DockingPort"/> if the part is a docking port, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public DockingPort DockingPort {
            get { return DockingPort.Is (this) ? new DockingPort (this) : null; }
        }

        /// <summary>
        /// An <see cref="Engine"/> if the part is an engine, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Engine Engine {
            get { return Engine.Is (this) ? new Engine (this) : null; }
        }

        /// <summary>
        /// A <see cref="LandingGear"/> if the part is a landing gear , otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public LandingGear LandingGear {
            get { return LandingGear.Is (this) ? new LandingGear (this) : null; }
        }

        /// <summary>
        /// A <see cref="LandingLeg"/> if the part is a landing leg, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public LandingLeg LandingLeg {
            get { return LandingLeg.Is (this) ? new LandingLeg (this) : null; }
        }

        /// <summary>
        /// A <see cref="LaunchClamp"/> if the part is a launch clamp, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public LaunchClamp LaunchClamp {
            get { return LaunchClamp.Is (this) ? new LaunchClamp (this) : null; }
        }

        /// <summary>
        /// A <see cref="Light"/> if the part is a light, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Light Light {
            get { return Light.Is (this) ? new Light (this) : null; }
        }

        /// <summary>
        /// A <see cref="Parachute"/> if the part is a parachute, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Parachute Parachute {
            get { return Parachute.Is (this) ? new Parachute (this) : null; }
        }

        /// <summary>
        /// A <see cref="Radiator"/> if the part is a radiator, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Radiator Radiator {
            get { return Radiator.Is (this) ? new Radiator (this) : null; }
        }

        /// <summary>
        /// A <see cref="ReactionWheel"/> if the part is a reaction wheel, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public ReactionWheel ReactionWheel {
            get { return ReactionWheel.Is (this) ? new ReactionWheel (this) : null; }
        }

        /// <summary>
        /// A <see cref="ResourceConverter"/> if the part is a resource converter, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public ResourceConverter ResourceConverter {
            get { return ResourceConverter.Is (this) ? new ResourceConverter (this) : null; }
        }

        /// <summary>
        /// A <see cref="ResourceHarvester"/> if the part is a resource harvester, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public ResourceHarvester ResourceHarvester {
            get { return ResourceHarvester.Is (this) ? new ResourceHarvester (this) : null; }
        }

        /// <summary>
        /// A <see cref="Sensor"/> if the part is a sensor, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Sensor Sensor {
            get { return Sensor.Is (this) ? new Sensor (this) : null; }
        }

        /// <summary>
        /// A <see cref="SolarPanel"/> if the part is a solar panel, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public SolarPanel SolarPanel {
            get { return SolarPanel.Is (this) ? new SolarPanel (this) : null; }
        }

        /// <summary>
        /// The position of the part in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (InternalPart.transform.position).ToTuple ();
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
            get { return ReferenceFrame.Object (InternalPart); }
        }
    }
}
