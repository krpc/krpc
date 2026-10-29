using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A battery, which stores electrical energy. Obtained from <see cref="Part.Batteries"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Battery : Equatable<Battery>, IGameObjectState
    {
        readonly global::KSA.Battery module;

        internal Battery (Part part, global::KSA.Battery module)
        {
            Part = part;
            this.module = module;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Battery other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (module, other.module);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Part.GameObjectState; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return module.GetHashCode ();
        }

        /// <summary>
        /// The part the battery is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        ref readonly BatteryState State {
            get {
                if (!ModuleStateful<global::KSA.Battery, BatteryState, EmptyStruct, EmptyStruct>.TryGetFrom (
                    Part.InternalVehicle.Parts.States, out var states))
                    throw new InvalidOperationException ("The vehicle has no batteries");
                return ref states.GetState (module);
            }
        }

        /// <summary>
        /// The energy stored in the battery, in joules.
        /// </summary>
        [KRPCProperty]
        public double StoredEnergy {
            get { return State.Charge.Value (); }
        }

        /// <summary>
        /// The energy the battery can store, in joules.
        /// </summary>
        [KRPCProperty]
        public double EnergyCapacity {
            get {
                _ = Part.InternalPart;
                return module.MaximumCapacity.Value ();
            }
        }
    }
}
