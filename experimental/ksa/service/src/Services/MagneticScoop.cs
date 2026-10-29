using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A magnetic scoop, which collects gas from space into the vehicle's tanks. Obtained from
    /// <see cref="Part.MagneticScoops"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class MagneticScoop : Equatable<MagneticScoop>, IGameObjectState
    {
        readonly global::KSA.MagneticScoop module;

        internal MagneticScoop (Part part, global::KSA.MagneticScoop module)
        {
            Part = part;
            this.module = module;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (MagneticScoop other)
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
        /// The part the scoop is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        /// <summary>
        /// Whether the scoop is switched on.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get {
                _ = Part.InternalPart;
                return module.IsOn;
            }
            set {
                _ = Part.InternalPart;
                module.IsOn = value;
            }
        }

        /// <summary>
        /// The name of the substance the scoop collects. See <see cref="Resource.Name"/>.
        /// </summary>
        [KRPCProperty]
        public string Substance {
            get {
                _ = Part.InternalPart;
                return module.Collects.Name;
            }
        }

        /// <summary>
        /// The rate the scoop collects gas at, in kilograms per second.
        /// </summary>
        [KRPCProperty]
        public double CollectionRate {
            get {
                if (!ModuleStateful<global::KSA.MagneticScoop, MagneticScoopState, EmptyStruct, EmptyStruct>.TryGetFrom (
                    Part.InternalVehicle.Parts.States, out var states))
                    throw new InvalidOperationException ("The vehicle has no magnetic scoops");
                return states.GetState (module).CollectionRate;
            }
        }
    }
}
