using System;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A habitable or storage space in a part, which holds meals and carbon dioxide scrubber
    /// canisters. Obtained from <see cref="Part.Habitats"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Habitat : Equatable<Habitat>, IGameObjectState
    {
        readonly global::KSA.Habitat module;

        internal Habitat (Part part, global::KSA.Habitat module)
        {
            Part = part;
            this.module = module;
        }

        internal static Habitat Of (global::KSA.Habitat module)
        {
            return new Habitat (new Part (module.FullPart), module);
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Habitat other)
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
        /// The part the habitat is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        internal global::KSA.Habitat InternalHabitat {
            get {
                _ = Part.InternalPart;
                return module;
            }
        }

        ReadOnlySpan<MoleState> MoleStates {
            get { return Part.InternalVehicle.Parts.Moles.States; }
        }

        /// <summary>
        /// The habitat zone the habitat is in.
        /// </summary>
        [KRPCProperty]
        public HabitatZone Zone {
            get {
                var vehicle = Part.InternalVehicle;
                vehicle.Parts.EnsureHabitatZones ();
                var zone = InternalHabitat.Zone ?? throw new InvalidOperationException ("The habitat is in no habitat zone");
                return new HabitatZone (new Vehicle (vehicle), zone.Lead);
            }
        }

        /// <summary>
        /// The volume the crew can live in, in cubic meters.
        /// </summary>
        [KRPCProperty]
        public float HabitableVolume {
            get { return InternalHabitat.HabitableVolume; }
        }

        /// <summary>
        /// The volume for stored supplies, in cubic meters.
        /// </summary>
        [KRPCProperty]
        public float StorageVolume {
            get { return InternalHabitat.StorageVolume; }
        }

        /// <summary>
        /// The number of meals in the habitat.
        /// </summary>
        [KRPCProperty]
        public int Meals {
            get { return InternalHabitat.CountMeals (MoleStates); }
        }

        /// <summary>
        /// The number of carbon dioxide scrubber canisters in the habitat.
        /// </summary>
        [KRPCProperty]
        public int Canisters {
            get { return InternalHabitat.CountCanisters (MoleStates); }
        }

        /// <summary>
        /// The number of meals the habitat holds when full.
        /// </summary>
        [KRPCProperty]
        public int MealCapacity {
            get {
                InternalHabitat.ComputeFullLoad (out var meals, out _);
                return meals;
            }
        }

        /// <summary>
        /// The number of carbon dioxide scrubber canisters the habitat holds when full.
        /// </summary>
        [KRPCProperty]
        public int CanisterCapacity {
            get {
                InternalHabitat.ComputeFullLoad (out _, out var canisters);
                return canisters;
            }
        }

        /// <summary>
        /// Moves meals and canisters to another habitat in the same zone. Moves fewer when
        /// this habitat holds fewer, or the other has less room.
        /// </summary>
        /// <param name="to">The habitat to move the supplies to.</param>
        /// <param name="meals">The number of meals to move.</param>
        /// <param name="canisters">The number of canisters to move.</param>
        /// <remarks>
        /// A kitten in the zone moves the supplies. Throws an exception if life support is
        /// disabled, the habitats are in different zones, or the zone has no crew able to
        /// move them.
        /// </remarks>
        [KRPCMethod]
        public void Transfer (Habitat to, int meals = 0, int canisters = 0)
        {
            if (ReferenceEquals (to, null))
                throw new ArgumentNullException (nameof (to));
            if (meals < 0 || canisters < 0)
                throw new ArgumentException ("The number of meals and canisters must not be negative");
            var from = InternalHabitat;
            var destination = to.InternalHabitat;
            if (ReferenceEquals (from, destination))
                throw new ArgumentException ("The habitats are the same");
            var vehicle = Part.InternalVehicle;
            vehicle.Parts.EnsureHabitatZones ();
            if (!Universe.Rules.LifeSupport)
                throw new InvalidOperationException ("Life support is disabled");
            var zone = from.Zone;
            if (zone == null || !ReferenceEquals (zone, destination.Zone))
                throw new InvalidOperationException ("The habitats are in different habitat zones");
            switch (global::KSA.Vehicle.FindSupplyCarrier (zone, Universe.GetElapsedTime ())) {
            case global::KSA.Vehicle.SupplyCarrier.NoCrew:
                throw new InvalidOperationException ("The habitat zone has no crew to move the supplies");
            case global::KSA.Vehicle.SupplyCarrier.AllIncapacitated:
                throw new InvalidOperationException ("The crew of the habitat zone are incapacitated");
            }
            if (meals == 0 && canisters == 0)
                return;
            var before = (Meals, Canisters);
            // As the game's habitat zones window moves supplies
            InputEvents.HabitatTransferBuffer.Add (new InputEvents.HabitatTransferData {
                Vehicle = vehicle,
                From = from,
                To = destination,
                Meals = meals,
                Canisters = canisters
            });
            Waits.Until (() => (Meals, Canisters) != before);
        }
    }
}
