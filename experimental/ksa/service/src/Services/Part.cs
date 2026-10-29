using System;
using System.Collections.Generic;
using System.Linq;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A part of a vehicle. Obtained from <see cref="Vehicle.Parts"/>.
    /// </summary>
    /// <remarks>
    /// A part keeps its object when the vehicle it belongs to changes, for example when a
    /// decoupler separates it. Throws an exception once the part no longer exists.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Part : Equatable<Part>, IGameObjectState
    {
        readonly global::KSA.Part part;

        internal Part (global::KSA.Part part)
        {
            this.part = part ?? throw new ArgumentNullException (nameof (part));
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Part other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (part, other.part);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return StateOf (part); }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        /// <summary>
        /// The KSA part. Throws an exception if it no longer exists.
        /// </summary>
        public global::KSA.Part InternalPart {
            get {
                var vehicle = part.Tree?.OwningVehicle;
                if (vehicle == null || !Vehicles.Exists (vehicle))
                    throw new Service.KRPC.ObjectDestroyedException ("The part no longer exists");
                return part;
            }
        }

        internal static GameObjectState StateOf (global::KSA.Part part)
        {
            return Vehicles.StateOf (part.Tree?.OwningVehicle);
        }

        internal global::KSA.Vehicle InternalVehicle {
            get { return InternalPart.Tree.OwningVehicle; }
        }

        /// <summary>
        /// The id of the part's template.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalPart.Template.Id; }
        }

        /// <summary>
        /// The name of the part, as shown in the game.
        /// </summary>
        [KRPCProperty]
        public string Title {
            get { return InternalPart.DisplayName; }
        }

        /// <summary>
        /// The vehicle the part belongs to.
        /// </summary>
        [KRPCProperty]
        public Vehicle Vehicle {
            get { return new Vehicle (InternalVehicle); }
        }

        /// <summary>
        /// The part this part is attached to, or <c>null</c> for the root part.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Part Parent {
            get {
                var parent = InternalPart.TreeParent;
                return parent == null ? null : new Part (parent);
            }
        }

        /// <summary>
        /// The parts attached to this part.
        /// </summary>
        [KRPCProperty]
        public IList<Part> Children {
            get { return InternalPart.TreeChildren.Select (child => new Part (child)).ToList (); }
        }

        /// <summary>
        /// The number of the sequence that activates the part, or -1 if no sequence does.
        /// </summary>
        [KRPCProperty]
        public int Sequence {
            get { return InternalPart.PrimarySequenceModule?.Sequence ?? -1; }
        }

        /// <summary>
        /// The mass of the part without its propellant, in kilograms.
        /// </summary>
        [KRPCProperty]
        public double DryMass {
            get {
                double mass = 0;
                foreach (var inert in InternalPart.SubtreeModules.Get<InertMass> ())
                    mass += inert.MassPropertiesAsmb.Props.Mass;
                return mass;
            }
        }

        /// <summary>
        /// The mass of the part, including its propellant, in kilograms.
        /// </summary>
        [KRPCProperty]
        public double Mass {
            get { return DryMass + Resources.Sum (resource => resource.Amount); }
        }

        /// <summary>
        /// The resources the part holds, one for each substance.
        /// </summary>
        [KRPCProperty]
        public IList<Resource> Resources {
            get { return Resource.Of (InternalVehicle, InternalPart); }
        }

        /// <summary>
        /// The position of the part's origin, in meters.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Position (ReferenceFrame referenceFrame)
        {
            return Vectors.ToTuple (referenceFrame.PositionFromEcl (PositionEcl));
        }

        /// <summary>
        /// The velocity of the part's origin, in meters per second.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the velocity.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Velocity (ReferenceFrame referenceFrame)
        {
            var vehicle = InternalVehicle;
            var positionEcl = PositionEcl;
            // The vehicle is rigid, so the part moves with its rotation about the center of mass
            var velocityEcl = vehicle.GetVelocityEcl () +
                double3.Cross (Vectors.AngularVelocityEcl (vehicle), positionEcl - vehicle.GetPositionEcl ());
            return Vectors.ToTuple (referenceFrame.VelocityFromEcl (positionEcl, velocityEcl));
        }

        double3 PositionEcl {
            get {
                var p = InternalPart;
                var vehicle = p.Tree.OwningVehicle;
                // The vehicle's position is its center of mass, in its body frame
                var centerOfMass = vehicle.TotalMassPropsAsmb.Offset;
                var offsetBody = p.PositionVehicleAsmb - new double3 (centerOfMass.X, centerOfMass.Y, centerOfMass.Z);
                return vehicle.GetPositionEcl () + offsetBody.Transform (vehicle.GetBodyFixed2Ecl ());
            }
        }

        /// <summary>
        /// The rotation of the part's axes, relative to the given frame.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the rotation.</param>
        [KRPCMethod]
        public Tuple<double, double, double, double> Rotation (ReferenceFrame referenceFrame)
        {
            var p = InternalPart;
            var part2Ecl = doubleQuat.Concatenate (p.Asmb2VehicleAsmb, p.Tree.OwningVehicle.GetBodyFixed2Ecl ());
            return Vectors.ToTuple (referenceFrame.RotationFromEcl (part2Ecl));
        }

        /// <summary>
        /// The part's engine, or <c>null</c> if it has none.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Engine Engine {
            get {
                var engines = InternalPart.SubtreeModules.Get<EngineController> ();
                return engines.Length == 0 ? null : new Engine (this, engines[0]);
            }
        }

        /// <summary>
        /// The part's gimbals. An engine part can have several, each steering one nozzle.
        /// </summary>
        [KRPCProperty]
        public IList<Gimbal> Gimbals {
            get { return InternalPart.SubtreeModules.Get<global::KSA.Gimbal> ().ToArray ().Select (gimbal => new Gimbal (this, gimbal)).ToList (); }
        }

        /// <summary>
        /// The part's propellant tanks. Empty if it has none.
        /// </summary>
        [KRPCProperty]
        public IList<Tank> Tanks {
            get { return InternalPart.SubtreeModules.Get<global::KSA.Tank> ().ToArray ().Select (tank => new Tank (this, tank)).ToList (); }
        }

        /// <summary>
        /// The part's batteries.
        /// </summary>
        [KRPCProperty]
        public IList<Battery> Batteries {
            get { return InternalPart.SubtreeModules.Get<global::KSA.Battery> ().ToArray ().Select (module => new Battery (this, module)).ToList (); }
        }

        /// <summary>
        /// The part's solar panels.
        /// </summary>
        [KRPCProperty]
        public IList<SolarPanel> SolarPanels {
            get { return InternalPart.SubtreeModules.Get<global::KSA.SolarPanel> ().ToArray ().Select (module => new SolarPanel (this, module)).ToList (); }
        }

        /// <summary>
        /// The part's generators.
        /// </summary>
        [KRPCProperty]
        public IList<Generator> Generators {
            get { return InternalPart.SubtreeModules.Get<global::KSA.Generator> ().ToArray ().Select (module => new Generator (this, module)).ToList (); }
        }

        /// <summary>
        /// The part's power consumers.
        /// </summary>
        [KRPCProperty]
        public IList<PowerConsumer> PowerConsumers {
            get { return InternalPart.SubtreeModules.Get<global::KSA.PowerConsumer> ().ToArray ().Select (module => new PowerConsumer (this, module)).ToList (); }
        }

        /// <summary>
        /// The part's lights.
        /// </summary>
        [KRPCProperty]
        public IList<Light> Lights {
            get { return InternalPart.SubtreeModules.Get<LightModule> ().ToArray ().Select (module => new Light (this, module)).ToList (); }
        }

        /// <summary>
        /// The part's RCS thrusters, or <c>null</c> if it has none.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public RCS RCS {
            get {
                var thrusters = InternalPart.SubtreeModules.Get<ThrusterController> ();
                return thrusters.Length == 0 ? null : new RCS (this, thrusters[0]);
            }
        }

        /// <summary>
        /// The part's decoupler, or <c>null</c> if it has none.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Decoupler Decoupler {
            get {
                var decouplers = InternalPart.SubtreeModules.Get<global::KSA.Decoupler> ();
                return decouplers.Length == 0 ? null : new Decoupler (this, decouplers[0]);
            }
        }

        /// <summary>
        /// The part's parachute, or <c>null</c> if it has none.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Parachute Parachute {
            get {
                var parachutes = InternalPart.SubtreeModules.Get<global::KSA.Parachute> ();
                return parachutes.Length == 0 ? null : new Parachute (this, parachutes[0]);
            }
        }

        /// <summary>
        /// The part's docking port, or <c>null</c> if it has none.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public DockingPort DockingPort {
            get {
                var ports = InternalPart.SubtreeModules.Get<global::KSA.DockingPort> ();
                return ports.Length == 0 ? null : new DockingPort (this, ports[0]);
            }
        }
    }
}
