using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The connected habitable parts of a vehicle, which share their crew's supplies.
    /// Obtained from <see cref="Vehicle.HabitatZones"/> or <see cref="Habitat.Zone"/>.
    /// </summary>
    /// <remarks>
    /// Parts join a zone through connections the crew can pass, such as an open docking
    /// port hatch.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class HabitatZone : Equatable<HabitatZone>, IGameObjectState
    {
        readonly Vehicle vehicle;
        // The game rebuilds its zones when the vehicle changes. The zone's first habitat
        // identifies it across rebuilds
        readonly global::KSA.Habitat lead;

        internal HabitatZone (Vehicle vehicle, global::KSA.Habitat lead)
        {
            this.vehicle = vehicle;
            this.lead = lead;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (HabitatZone other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (lead, other.lead);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = vehicle.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                return Find () == null ? GameObjectState.Destroyed : GameObjectState.Live;
            }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return lead.GetHashCode ();
        }

        global::KSA.HabitatZone Find ()
        {
            foreach (var zone in vehicle.InternalVehicle.Parts.EnsureHabitatZones ()) {
                if (ReferenceEquals (zone.Lead, lead))
                    return zone;
            }
            return null;
        }

        internal global::KSA.HabitatZone InternalZone {
            get { return Find () ?? throw new Service.KRPC.ObjectDestroyedException ("The habitat zone no longer exists"); }
        }

        /// <summary>
        /// The vehicle the zone is in.
        /// </summary>
        [KRPCProperty]
        public Vehicle Vehicle {
            get { return vehicle; }
        }

        /// <summary>
        /// The parts in the zone.
        /// </summary>
        [KRPCProperty]
        public IList<Part> Parts {
            get { return InternalZone.Parts.Select (part => new Part (part)).ToList (); }
        }

        /// <summary>
        /// The habitats in the zone.
        /// </summary>
        [KRPCProperty]
        public IList<Habitat> Habitats {
            get { return InternalZone.Habitats.Select (Habitat.Of).ToList (); }
        }

        /// <summary>
        /// The crew seats in the zone.
        /// </summary>
        [KRPCProperty]
        public IList<Seat> Seats {
            get { return InternalZone.Seats.Select (seat => new Seat (new Part (seat.Parent.FullPart), seat)).ToList (); }
        }

        /// <summary>
        /// The kittens in the zone's seats.
        /// </summary>
        [KRPCProperty]
        public IList<Kitten> Crew {
            get { return Seats.Select (seat => seat.Kitten).Where (kitten => kitten != null).ToList (); }
        }

        /// <summary>
        /// The volume the crew can live in, in cubic meters.
        /// </summary>
        [KRPCProperty]
        public float HabitableVolume {
            get { return InternalZone.HabitableVolume; }
        }

        /// <summary>
        /// The volume for stored supplies, in cubic meters.
        /// </summary>
        [KRPCProperty]
        public float StorageVolume {
            get { return InternalZone.StorageVolume; }
        }

        /// <summary>
        /// How long the zone's supplies last its crew.
        /// </summary>
        [KRPCProperty]
        public LifeSupport LifeSupport {
            get { return new LifeSupport (vehicle, this); }
        }
    }
}
