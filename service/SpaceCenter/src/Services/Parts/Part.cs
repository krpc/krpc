using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CompoundParts;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Represents an individual part. Vessels are made up of multiple parts.
    /// Instances of this class can be obtained by several methods in <see cref="Parts"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Part : Equatable<Part>
    {
        readonly uint partFlightId;

        /// <summary>
        /// Create a part object for the given KSP part
        /// </summary>
        public Part (global::Part part)
        {
            if (ReferenceEquals (part, null))
                throw new ArgumentNullException (nameof (part));
            partFlightId = part.flightID;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Part other)
        {
            return !ReferenceEquals (other, null) && partFlightId == other.partFlightId;
        }

        /// <summary>
        /// Hash code for the object.
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
        /// The name tag for the part. Can be set to a custom string using the in-game user interface.
        /// </summary>
        /// <remarks>
        /// This requires either the <a href="http://github.com/krpc/NameTag/releases/latest">NameTag</a> or
        /// <a href="http://forum.kerbalspaceprogram.com/index.php?/topic/61827-/">kOS</a> mods to be installed.
        /// </remarks>
        [KRPCProperty]
        public string Tag {
            get {
                var module = InternalPart.Module ("KOSNameTag");
                if (module == null) {
                    throw new InvalidOperationException ("NameTag mod is not installed");
                }
                return (string)module.GetType ().GetField ("nameTag").GetValue (module);
            }
            set {
                var module = InternalPart.Module ("KOSNameTag");
                if (module == null)
                    throw new InvalidOperationException ("NameTag mod is not installed");
                module.GetType ().GetField ("nameTag").SetValue (module, value);
            }
        }

        /// <summary>
        /// Whether the part is highlighted.
        /// </summary>
        [KRPCProperty]
        public bool Highlighted {
            get { return InternalPart.HighlightActive; }
            set {
                var part = InternalPart;
                if (value)
                    PartHighlightAddon.Add (part);
                else
                    PartHighlightAddon.Remove (part);
            }
        }

        /// <summary>
        /// The color used to highlight the part.
        /// </summary>
        [KRPCProperty]
        public Tuple3 HighlightColor {
            get { return InternalPart.highlightColor.ToTuple (); }
            set { InternalPart.highlightColor = value.ToColor (); }
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

        bool HasParent {
            get { return InternalPart.parent != null; }
        }

        /// <summary>
        /// The parts parent. Returns <c>null</c> if the part does not have a parent.
        /// This, in combination with <see cref="Children"/>, can be used to traverse the vessels parts tree.
        /// </summary>
        [KRPCProperty]
        public Part Parent {
            get { return HasParent ? new Part (InternalPart.parent) : null; }
        }

        /// <summary>
        /// The parts children. Returns an empty list if the part has no children.
        /// This, in combination with <see cref="Parent"/>, can be used to traverse the vessels parts tree.
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public bool AxiallyAttached {
            get { return HasParent && InternalPart.attachMode == AttachModes.STACK; }
        }

        /// <summary>
        /// Whether the part is radially attached to its parent, i.e. on the side of its parent.
        /// If the part has no parent, returns <c>false</c>.
        /// </summary>
        [KRPCProperty]
        public bool RadiallyAttached {
            get { return HasParent && InternalPart.attachMode == AttachModes.SRF_ATTACH; }
        }

        /// <summary>
        /// The stage in which this part will be activated. Returns -1 if the part is not activated by staging.
        /// </summary>
        [KRPCProperty]
        public int Stage {
            get {
                var part = InternalPart;
                return part.hasStagingIcon ? part.inverseStage : -1;
            }
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
            get { return InternalPart.IsMassless (); }
        }

        /// <summary>
        /// The current mass of the part, including resources it contains, in kilograms.
        /// Returns zero if the part is massless.
        /// </summary>
        [KRPCProperty]
        public double Mass {
            get { return InternalPart.WetMass (); }
        }

        /// <summary>
        /// The mass of the part, not including any resources it contains, in kilograms. Returns zero if the part is massless.
        /// </summary>
        [KRPCProperty]
        public double DryMass {
            get { return InternalPart.DryMass (); }
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

        void CheckPartIsNotAFuelLine ()
        {
            if (IsFuelLine)
                throw new InvalidOperationException ("Part is a fuel line");
        }

        /// <summary>
        /// The parts that are connected to this part via fuel lines, where the direction of the fuel line is into this part.
        /// </summary>
        [KRPCProperty]
        public IList<Part> FuelLinesFrom {
            get {
                CheckPartIsNotAFuelLine ();
                return InternalPart.fuelLookupTargets.Select (x => new Part (x.parent)).ToList ();
            }
        }

        /// <summary>
        /// The parts that are connected to this part via fuel lines, where the direction of the fuel line is out of this part.
        /// </summary>
        [KRPCProperty]
        public IList<Part> FuelLinesTo {
            get {
                CheckPartIsNotAFuelLine ();
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
        /// A <see cref="Antenna"/> if the part is an antenna, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Antenna Antenna {
            get { return Antenna.Is (this) ? new Antenna (this) : null; }
        }

        /// <summary>
        /// A <see cref="CargoBay"/> if the part is a cargo bay, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public CargoBay CargoBay {
            get { return CargoBay.Is (this) ? new CargoBay (this) : null; }
        }

        /// <summary>
        /// A <see cref="ControlSurface"/> if the part is an aerodynamic control surface, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public ControlSurface ControlSurface {
            get { return ControlSurface.Is (this) ? new ControlSurface (this) : null; }
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
        /// An <see cref="Experiment"/> if the part is a science experiment, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Experiment Experiment {
            get { return Experiment.Is (this) ? new Experiment (this) : null; }
        }

        /// <summary>
        /// A <see cref="Fairing"/> if the part is a fairing, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Fairing Fairing {
            get { return Fairing.Is (this) ? new Fairing (this) : null; }
        }

        /// <summary>
        /// An <see cref="Intake"/> if the part is an intake, otherwise <c>null</c>.
        /// </summary>
        /// <remarks>
        /// This includes any part that generates thrust. This covers many different types of engine,
        /// including liquid fuel rockets, solid rocket boosters and jet engines.
        /// For RCS thrusters see <see cref="RCS"/>.
        /// </remarks>
        [KRPCProperty]
        public Intake Intake {
            get { return Intake.Is (this) ? new Intake (this) : null; }
        }

        /// <summary>
        /// A <see cref="Leg"/> if the part is a landing leg, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Leg Leg {
            get { return Leg.Is (this) ? new Leg (this) : null; }
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
        /// A <see cref="RCS"/> if the part is an RCS block/thruster, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public RCS RCS {
            get { return RCS.Is (this) ? new RCS (this) : null; }
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
        /// A <see cref="Wheel"/> if the part is a wheel, otherwise <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Wheel Wheel {
            get { return Wheel.Is (this) ? new Wheel (this) : null; }
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
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
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
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.PositionFromWorldSpace (InternalPart.CenterOfMass ()).ToTuple ();
        }

        /// <summary>
        /// The axis-aligned bounding box of the vessel in the given reference frame.
        /// Returns the minimum and maximum vertices of the box.
        /// </summary>
        /// <param name="referenceFrame"></param>
        /// <remarks>
        /// This is computed from the collision meshes of the part.
        /// If the part is not collidable, the box has zero volume and is centered on
        /// the <see cref="Position"/> of the part.
        /// </remarks>
        [KRPCMethod]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        public Tuple<Tuple3,Tuple3> BoundingBox (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return InternalPart.GetBounds (referenceFrame).ToTuples ();
        }

        /// <summary>
        /// The direction of the part in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (InternalPart.transform.up).ToTuple ();
        }

        /// <summary>
        /// The velocity of the part in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Velocity (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var part = InternalPart;
            return referenceFrame.VelocityFromWorldSpace (part.transform.position, part.orbit.GetVel ()).ToTuple ();
        }

        /// <summary>
        /// The rotation of the part in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.RotationFromWorldSpace (InternalPart.transform.rotation).ToTuple ();
        }

        /// <summary>
        /// The moment of inertia of the part in <math>kg.m^2</math> around its center of mass
        /// in the parts reference frame (<see cref="ReferenceFrame"/>).
        /// </summary>
        [KRPCProperty]
        public Tuple3 MomentOfInertia {
            get { return ComputeInertiaTensor ().Diagonal ().ToTuple (); }
        }

        /// <summary>
        /// The inertia tensor of the part in the parts reference frame (<see cref="ReferenceFrame"/>).
        /// Returns the 3x3 matrix as a list of elements, in row-major order.
        /// </summary>
        [KRPCProperty]
        public IList<double> InertiaTensor {
            get { return ComputeInertiaTensor ().ToList (); }
        }

        /// <summary>
        /// Computes the inertia tensor of the part in the parts reference frame.
        /// </summary>
        Matrix4x4 ComputeInertiaTensor ()
        {
            var part = InternalPart;
            if (part.rb == null)
                return Matrix4x4.zero;

            Matrix4x4 partTensor = part.rb.inertiaTensor.ToDiagonalMatrix ();

            // translate: inertiaTensor frame to part frame
            Quaternion rot = part.rb.inertiaTensorRotation;
            Quaternion inv = Quaternion.Inverse (rot);

            Matrix4x4 rotMatrix = Matrix4x4.TRS (Vector3.zero, rot, Vector3.one);
            Matrix4x4 invMatrix = Matrix4x4.TRS (Vector3.zero, inv, Vector3.one);

            var inertiaTensor = rotMatrix * partTensor * invMatrix;
            return inertiaTensor.MultiplyScalar (1000f);
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

        /// <summary>
        /// Exert a constant force on the part, acting at the given position.
        /// Returns an object that can be used to remove or modify the force.
        /// </summary>
        [KRPCMethod]
        public Force AddForce (Tuple3 force, Tuple3 position, ReferenceFrame referenceFrame)
        {
            var obj = new Force (this, force, position, referenceFrame);
            PartForcesAddon.Add (obj);
            return obj;
        }

        /// <summary>
        /// Exert an instantaneous force on the part, acting at the given position.
        /// </summary>
        /// <remarks>
        /// The force is applied instantaneously in a single physics update.
        /// </remarks>
        [KRPCMethod]
        public void InstantaneousForce (Tuple3 force, Tuple3 position, ReferenceFrame referenceFrame)
        {
            PartForcesAddon.AddInstantaneous (new Force (this, force, position, referenceFrame));
        }
    }
}
