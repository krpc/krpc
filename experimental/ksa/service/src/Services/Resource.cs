using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A substance held in tanks, such as a propellant, in a part or across a whole vehicle.
    /// Obtained from <see cref="Part.Resources"/> or <see cref="Vehicle.Resources"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Resource : Equatable<Resource>, IGameObjectState
    {
        readonly global::KSA.Vehicle vehicle;
        readonly global::KSA.Part part;

        Resource (global::KSA.Vehicle vehicle, global::KSA.Part part, string name)
        {
            this.vehicle = vehicle;
            this.part = part;
            Name = name;
        }

        /// <summary>
        /// The resources of the given part, or of the whole vehicle when the part is null, one
        /// for each substance.
        /// </summary>
        internal static IList<Resource> Of (global::KSA.Vehicle vehicle, global::KSA.Part part)
        {
            return Moles (vehicle, part).Select (mole => mole.SubstancePhase.Name).Distinct ()
                .Select (name => new Resource (vehicle, part, name)).ToList ();
        }

        static IEnumerable<Mole> Moles (global::KSA.Vehicle vehicle, global::KSA.Part part)
        {
            return (part == null ? vehicle.Parts.Moles.Modules : part.SubtreeModules.Get<Mole> ()).ToArray ();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Resource other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (vehicle, other.vehicle) &&
                ReferenceEquals (part, other.part) && Name == other.Name;
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return part == null ? Vehicles.StateOf (vehicle) : Part.StateOf (part); }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return HashCode.Combine (vehicle, part, Name);
        }

        /// <summary>
        /// The name of the substance.
        /// </summary>
        [KRPCProperty]
        public string Name { get; }

        /// <summary>
        /// The mass of the substance held, in kilograms.
        /// </summary>
        [KRPCProperty]
        public double Amount {
            get { return Sum ((mole, state) => state.Mass); }
        }

        /// <summary>
        /// The mass of the substance the tanks can hold, in kilograms.
        /// </summary>
        [KRPCProperty]
        public double Max {
            get { return Sum ((mole, state) => mole.GetStoredMass (mole.ContainerVolume)); }
        }

        double Sum (Func<Mole, MoleState, double> value)
        {
            var v = part == null ? new Vehicle (vehicle).InternalVehicle : new Part (part).InternalVehicle;
            var states = v.Parts.Moles;
            double total = 0;
            foreach (var mole in Moles (v, part)) {
                if (mole.SubstancePhase.Name == Name)
                    total += value (mole, states.GetState (mole));
            }
            return total;
        }
    }
}
