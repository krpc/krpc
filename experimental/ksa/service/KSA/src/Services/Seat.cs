using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A crew seat. Obtained from <see cref="Part.Seats"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Seat : Equatable<Seat>, IGameObjectState
    {
        readonly IVASeat module;

        internal Seat (Part part, IVASeat module)
        {
            Part = part;
            this.module = module;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Seat other)
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
        /// The part the seat is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        internal IVASeat InternalSeat {
            get {
                _ = Part.InternalPart;
                return module;
            }
        }

        /// <summary>
        /// Returns the kitten in the seat, or <c>null</c> if the seat is empty, and sets the
        /// kitten in the seat. Setting <c>null</c> empties the seat.
        /// </summary>
        /// <remarks>
        /// A kitten in another seat of the vehicle moves to this one. Throws an exception if
        /// the kitten is killed in action, on EVA, or in another vehicle.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public Kitten Kitten {
            get {
                var hash = InternalSeat.AssignedKittenHash;
                if (hash == KeyHash.Zero)
                    return null;
                var entry = Universe.KittenRoster.Find (hash);
                return entry == null ? null : new Kitten (entry.Name);
            }
            set {
                var seat = InternalSeat;
                var vehicle = Part.InternalVehicle;
                if (ReferenceEquals (value, null)) {
                    global::KSA.Vehicle.SetSeatCrew (seat, KeyHash.Zero, vehicle.Id, vehicle.HasLaunched);
                    return;
                }
                var entry = value.Entry;
                if (entry.Kia)
                    throw new InvalidOperationException ("The kitten was killed in action");
                if (value.OnEva)
                    throw new InvalidOperationException ("The kitten is on EVA");
                var current = value.Seat;
                if (current != null) {
                    if (current.Equals (this))
                        return;
                    if (!ReferenceEquals (current.Part.InternalVehicle, vehicle))
                        throw new InvalidOperationException ("The kitten is in another vehicle");
                    current.module.AssignedKittenHash = KeyHash.Zero;
                }
                global::KSA.Vehicle.SetSeatCrew (seat, entry.NameHash, vehicle.Id, vehicle.HasLaunched);
            }
        }
    }
}
