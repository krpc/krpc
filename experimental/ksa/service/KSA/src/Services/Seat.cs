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
        /// A kitten in another seat of the vehicle moves to this one, within its habitat zone.
        /// Throws an exception if the kitten is killed in action, retired, on EVA, in another
        /// vehicle, or incapacitated, or the seat is in another habitat zone.
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
                if (entry.Retired)
                    throw new InvalidOperationException ("The kitten has retired");
                if (value.OnEva)
                    throw new InvalidOperationException ("The kitten is on EVA");
                var current = value.Seat;
                if (current == null) {
                    global::KSA.Vehicle.SetSeatCrew (seat, entry.NameHash, vehicle.Id, vehicle.HasLaunched);
                    return;
                }
                if (current.Equals (this))
                    return;
                if (!ReferenceEquals (current.Part.InternalVehicle, vehicle))
                    throw new InvalidOperationException ("The kitten is in another vehicle");
                Move (vehicle, current.module, seat, entry);
            }
        }

        // Move a kitten aboard to another seat, as the game's crew window does. A kitten in
        // the seat leaves the vehicle, and returns to the seat if the move fails
        static void Move (global::KSA.Vehicle vehicle, IVASeat from, IVASeat to, KittenRosterEntryData entry)
        {
            var tree = vehicle.Parts;
            switch (global::KSA.Vehicle.CompareZones (tree, from, to)) {
            case ZoneMatch.NoZone:
                throw new InvalidOperationException ("The seat is in no habitat zone");
            case ZoneMatch.Different:
                throw new InvalidOperationException ("The seat is in another habitat zone");
            }
            if (Universe.Rules.LifeSupport && entry.Condition.IsIncapacitated (Universe.GetElapsedTime ()))
                throw new InvalidOperationException ("The kitten is incapacitated");
            // The game moves a kitten only into an empty seat
            var occupant = to.AssignedKittenHash;
            if (occupant != KeyHash.Zero)
                global::KSA.Vehicle.SetSeatCrew (to, KeyHash.Zero, vehicle.Id, vehicle.HasLaunched);
            var result = global::KSA.Vehicle.MoveCrew (tree, entry.NameHash, to);
            if (result != CrewMoveResult.Moved) {
                if (occupant != KeyHash.Zero)
                    global::KSA.Vehicle.SetSeatCrew (to, occupant, vehicle.Id, vehicle.HasLaunched);
                throw new InvalidOperationException ("The kitten could not move seats: " + result);
            }
        }
    }
}
