using System;
using KRPC.KSA.Utils;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A kitten in the agency's roster. Obtained from <see cref="KSA.Kittens"/>,
    /// <see cref="Vehicle.Crew"/> or <see cref="Seat.Kitten"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Kitten : Equatable<Kitten>, IGameObjectState
    {
        // The roster identifies kittens by name, and a save load replaces the roster entries
        readonly string name;

        internal Kitten (string name)
        {
            this.name = name ?? throw new ArgumentNullException (nameof (name));
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Kitten other)
        {
            return !ReferenceEquals (other, null) && name == other.name;
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Find (name) != null ? GameObjectState.Live : GameObjectState.Destroyed; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return name.GetHashCode (StringComparison.Ordinal);
        }

        internal static KittenRosterEntryData Find (string name)
        {
            return Universe.CurrentSystem == null ? null : Universe.KittenRoster?.Find (name);
        }

        /// <summary>
        /// The kitten's roster entry. Throws an exception if the roster has no kitten with
        /// this name.
        /// </summary>
        internal KittenRosterEntryData Entry {
            get { return Find (name) ?? throw new Service.KRPC.ObjectDestroyedException ("The kitten no longer exists"); }
        }

        /// <summary>
        /// The kitten's name, which is unique in the roster.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get {
                _ = Entry;
                return name;
            }
        }

        /// <summary>
        /// The kitten's position in the agency's roster, from 0. See <see cref="KSA.Kittens"/>.
        /// </summary>
        [KRPCProperty]
        public int RosterIndex {
            get { return Universe.KittenRoster.Kittens.IndexOf (Entry); }
        }

        /// <summary>
        /// The seat the kitten is in, or <c>null</c> if it is not in a seat.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Seat Seat {
            get {
                var hash = Entry.NameHash;
                foreach (var vehicle in Vehicles.All) {
                    foreach (var seat in vehicle.Crew) {
                        if (seat.AssignedKittenHash == hash)
                            return new Seat (new Part (seat.Parent.FullPart), seat);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// The vehicle the kitten is in, or <c>null</c> if it is not in a seat.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Vehicle Vehicle {
            get { return Seat?.Part.Vehicle; }
        }

        /// <summary>
        /// Whether the kitten has been killed in action.
        /// </summary>
        [KRPCProperty]
        public bool KilledInAction {
            get { return Entry.Kia; }
        }

        /// <summary>
        /// What killed the kitten, or <c>null</c> if it is alive.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public DeathCause? CauseOfDeath {
            get {
                var entry = Entry;
                return entry.Kia ? (DeathCause)entry.CauseOfDeath : null;
            }
        }

        /// <summary>
        /// The time the kitten has gone without a supply, in seconds. It counts down again
        /// once the supply returns.
        /// </summary>
        /// <param name="supply">The supply.</param>
        [KRPCMethod]
        public float TimeWithout (LifeSupportSupply supply)
        {
            return (float)Entry.Condition.GetTimeWithout ((global::KSA.LifeSupportSupply)supply, Universe.GetElapsedTime ()).Seconds ();
        }

        /// <summary>
        /// The effect on the kitten of going without a supply.
        /// </summary>
        /// <param name="supply">The supply.</param>
        [KRPCMethod]
        public KittenCondition Condition (LifeSupportSupply supply)
        {
            return (KittenCondition)Entry.Condition.GetRung ((global::KSA.LifeSupportSupply)supply, Universe.GetElapsedTime ());
        }

        /// <summary>
        /// The kitten's worst condition across its supplies.
        /// </summary>
        [KRPCProperty]
        public KittenCondition WorstCondition {
            get { return (KittenCondition)Entry.Condition.GetWorstRung (Universe.GetElapsedTime (), out _); }
        }

        /// <summary>
        /// Whether the kitten is incapacitated by going without a supply. An incapacitated
        /// kitten cannot fly the vehicle or move supplies.
        /// </summary>
        [KRPCProperty]
        public bool Incapacitated {
            get { return Entry.Condition.IsIncapacitated (Universe.GetElapsedTime ()); }
        }

        /// <summary>
        /// The number of missions the kitten has flown, including the current one.
        /// </summary>
        [KRPCProperty]
        public int MissionCount {
            get { return Entry.MissionCount; }
        }

        /// <summary>
        /// The time the kitten has spent on missions, including the current one, in seconds.
        /// </summary>
        [KRPCProperty]
        public float MissionTime {
            get {
                var entry = Entry;
                var total = ((UniverseTime)entry.TotalMissionTime).Seconds ();
                // The start time is set once the kitten's vehicle launches
                if (entry.AssignedToVehicle && entry.CurrentMissionStartTime.IsValid ())
                    total += (Universe.GetElapsedTime () - entry.CurrentMissionStartTime).Seconds ();
                return (float)total;
            }
        }

        /// <summary>
        /// The distance the kitten has traveled on missions, in meters.
        /// </summary>
        [KRPCProperty]
        public float DistanceTraveled {
            get { return (float)Entry.TravelledMeters.InMeters (); }
        }

        /// <summary>
        /// The kitten's highest speed on a mission, in meters per second.
        /// </summary>
        [KRPCProperty]
        public float FastestSpeed {
            // The game stores the speed in a distance field
            get { return (float)Entry.FastestSpeed.InMeters (); }
        }

        /// <summary>
        /// The kitten's acute radiation dose, in sieverts. It heals over time, and causes
        /// radiation sickness from 1 sievert and death from 4 sieverts.
        /// </summary>
        [KRPCProperty]
        public float AcuteDose {
            get { return (float)Entry.GetAcuteDose (Universe.GetElapsedTime ()).Value (); }
        }

        /// <summary>
        /// The kitten's lifetime radiation dose, in sieverts. The kitten retires after a
        /// mission that takes it past 0.6 sieverts.
        /// </summary>
        [KRPCProperty]
        public float LifetimeDose {
            get { return (float)Entry.LifetimeDose.Value (); }
        }

        /// <summary>
        /// The radiation dose rate the kitten receives, in sieverts per second. The vehicle's
        /// shielding reduces it in a seat, and on EVA the kitten is unshielded. Zero when the
        /// kitten is not on a mission.
        /// </summary>
        [KRPCProperty]
        public float DoseRate {
            get { return (float)Entry.DoseRate.Value (); }
        }

        /// <summary>
        /// Whether the kitten has radiation sickness, which keeps it from flying.
        /// </summary>
        [KRPCProperty]
        public bool RadiationSickness {
            get { return Entry.HasRadiationSickness (Universe.GetElapsedTime ()); }
        }

        /// <summary>
        /// The time until the kitten has recovered from radiation sickness and can fly, resting
        /// at home, in seconds. Zero if the kitten does not have radiation sickness.
        /// </summary>
        [KRPCProperty]
        public float TimeUntilFitToFly {
            get { return (float)Entry.GetTimeUntilFitToFly (Universe.GetElapsedTime ()); }
        }

        /// <summary>
        /// The time until the kitten's acute radiation dose has healed below 1 millisievert,
        /// resting at home, in seconds.
        /// </summary>
        [KRPCProperty]
        public float RecoveryTime {
            get { return (float)Entry.GetRecoveryTime (Universe.GetElapsedTime ()); }
        }

        /// <summary>
        /// Whether the kitten has retired, having reached its lifetime radiation dose limit.
        /// </summary>
        [KRPCProperty]
        public bool Retired {
            get { return Entry.Retired; }
        }

        /// <summary>
        /// The kitten on EVA, or <c>null</c> if the kitten is not on EVA.
        /// </summary>
        internal KittenEva Eva {
            get {
                var system = Universe.CurrentSystem;
                return system != null && system.All.TryGet (name, out Astronomical eva) ? eva as KittenEva : null;
            }
        }

        KittenEva EvaOrThrow {
            get {
                _ = Entry;
                return Eva ?? throw new InvalidOperationException ("The kitten is not on EVA");
            }
        }

        /// <summary>
        /// Whether the kitten is on EVA.
        /// </summary>
        [KRPCProperty]
        public bool OnEva {
            get {
                _ = Entry;
                return Eva != null;
            }
        }

        /// <summary>
        /// Leave the kitten's vehicle through its EVA hatch, and return once the kitten is on
        /// EVA. The kitten becomes the controlled kitten, see <see cref="KSA.ActiveKitten"/>.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the kitten is not in a seat, or the vehicle has no EVA hatch.
        /// </remarks>
        [KRPCMethod]
        public void GoOnEva ()
        {
            var seat = Seat ?? throw new InvalidOperationException ("The kitten is not in a seat");
            var vehicle = seat.Part.InternalVehicle;
            // As the game's EVA button checks
            switch (vehicle.CheckEva (Entry, seat.InternalSeat)) {
            case EvaCheck.Incapacitated:
                throw new InvalidOperationException ("The kitten is incapacitated");
            case EvaCheck.Degraded:
                throw new InvalidOperationException ("The kitten is too weak to go on EVA");
            case EvaCheck.NoSuitSupply:
                throw new InvalidOperationException ("The habitat zone has no supplies to fill the kitten's suit");
            }
            switch (EVADoor.PerformEvaForSeat (vehicle, seat.InternalSeat)) {
            case EvaRequest.NoDoor:
                throw new InvalidOperationException ("The vehicle has no EVA hatch");
            case EvaRequest.EmptySeat:
            case EvaRequest.NotOnRoster:
                throw new InvalidOperationException ("The kitten is not in the seat");
            }
            WaitFor (() => OnEva, "The kitten did not go on EVA", 10);
        }

        /// <summary>
        /// Whether the kitten can go on EVA. See <see cref="GoOnEva"/>.
        /// </summary>
        [KRPCProperty]
        public bool CanGoOnEva {
            get {
                var seat = Seat;
                if (seat == null)
                    return false;
                var vehicle = seat.Part.InternalVehicle;
                return vehicle.CheckEva (Entry, seat.InternalSeat) == EvaCheck.Ready && !vehicle.Parts.EVADoors.Modules.IsEmpty;
            }
        }

        /// <summary>
        /// How long the supplies in the kitten's suit last, or <c>null</c> if the kitten is
        /// not on EVA.
        /// </summary>
        /// <remarks>
        /// The suit is filled from the kitten's habitat zone when it goes on EVA, and what is
        /// left returns to the zone when it boards.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public LifeSupport LifeSupport {
            get {
                _ = Entry;
                var eva = Eva;
                return eva == null ? null : new LifeSupport (new Vehicle (eva));
            }
        }

        /// <summary>
        /// Board the nearest vehicle with a free seat, and return once the kitten is in the
        /// seat. The vehicle becomes the controlled vehicle.
        /// </summary>
        /// <returns>The vehicle the kitten boarded.</returns>
        /// <remarks>
        /// Throws an exception if the kitten is not on EVA, or no EVA hatch with a free seat is
        /// within one meter.
        /// </remarks>
        [KRPCMethod]
        public Vehicle Board ()
        {
            var eva = EvaOrThrow;
            var (status, host, door) = eva.FindBoardTarget ();
            if (status != BoardTargetStatus.Found)
                throw new InvalidOperationException ("No EVA hatch with a free seat is in range");
            InputEvents.KittenBoardBuffer.Add (new InputEvents.KittenBoardData { Kitten = eva, Host = host, Door = door });
            return WaitFor (() => !OnEva, () => new Vehicle (host), "The kitten did not board the vehicle", 10);
        }

        /// <summary>
        /// Whether the kitten is on EVA and an EVA hatch with a free seat is in range. See
        /// <see cref="Board"/>.
        /// </summary>
        [KRPCProperty]
        public bool CanBoard {
            get {
                _ = Entry;
                var eva = Eva;
                return eva != null && eva.FindBoardTarget ().Status == BoardTargetStatus.Found;
            }
        }

        global::KSA.Habitat Cargo {
            get { return EvaCarry.FindCargo (EvaOrThrow.Parts) ?? throw new InvalidOperationException ("The kitten has no space to carry supplies"); }
        }

        /// <summary>
        /// The number of meals the kitten on EVA carries. Throws an exception if the kitten is
        /// not on EVA.
        /// </summary>
        [KRPCProperty]
        public int CarriedMeals {
            get { return EvaCarry.Count (Cargo, EvaOrThrow.Parts.Moles.States).Meals; }
        }

        /// <summary>
        /// The number of carbon dioxide scrubber canisters the kitten on EVA carries. Throws an
        /// exception if the kitten is not on EVA.
        /// </summary>
        [KRPCProperty]
        public int CarriedCanisters {
            get { return EvaCarry.Count (Cargo, EvaOrThrow.Parts.Moles.States).Canisters; }
        }

        /// <summary>
        /// Take meals and canisters from a habitat within reach, and return once the kitten
        /// carries them. Takes fewer when the habitat holds fewer, or the kitten cannot carry
        /// more.
        /// </summary>
        /// <param name="habitat">The habitat, which must store supplies.</param>
        /// <param name="meals">The number of meals to take.</param>
        /// <param name="canisters">The number of canisters to take.</param>
        /// <remarks>
        /// Throws an exception if the kitten is not on EVA or is unwell, life support is
        /// disabled, or the habitat stores no supplies or is out of reach. See
        /// <see cref="CanCarry"/>.
        /// </remarks>
        [KRPCMethod]
        public void Take (Habitat habitat, int meals = 0, int canisters = 0)
        {
            Carry (habitat, CarryDirection.Take, meals, canisters);
        }

        /// <summary>
        /// Stow the meals and canisters the kitten carries in a habitat within reach, and
        /// return once they are stowed. Stows fewer when the kitten carries fewer, or the
        /// habitat has less room.
        /// </summary>
        /// <param name="habitat">The habitat, which must store supplies.</param>
        /// <param name="meals">The number of meals to stow.</param>
        /// <param name="canisters">The number of canisters to stow.</param>
        /// <remarks>
        /// Throws an exception if the kitten is not on EVA or is unwell, life support is
        /// disabled, or the habitat stores no supplies or is out of reach. See
        /// <see cref="CanCarry"/>.
        /// </remarks>
        [KRPCMethod]
        public void Stow (Habitat habitat, int meals = 0, int canisters = 0)
        {
            Carry (habitat, CarryDirection.Stow, meals, canisters);
        }

        /// <summary>
        /// Whether the kitten can take supplies from or stow them in a habitat. See
        /// <see cref="Take"/> and <see cref="Stow"/>.
        /// </summary>
        /// <param name="habitat">The habitat.</param>
        [KRPCMethod]
        public bool CanCarry (Habitat habitat)
        {
            if (ReferenceEquals (habitat, null))
                throw new ArgumentNullException (nameof (habitat));
            _ = Entry;
            var eva = Eva;
            return eva != null && eva.CheckCarry (habitat.Part.InternalVehicle, habitat.InternalHabitat) == CarryCheck.Ready;
        }

        void Carry (Habitat habitat, CarryDirection direction, int meals, int canisters)
        {
            if (ReferenceEquals (habitat, null))
                throw new ArgumentNullException (nameof (habitat));
            if (meals < 0 || canisters < 0)
                throw new ArgumentException ("The number of meals and canisters must not be negative");
            var eva = EvaOrThrow;
            var host = habitat.Part.InternalVehicle;
            var module = habitat.InternalHabitat;
            switch (eva.CheckCarry (host, module)) {
            case CarryCheck.LifeSupportOff:
                throw new InvalidOperationException ("Life support is disabled");
            case CarryCheck.NoCargoSpace:
                throw new InvalidOperationException ("The kitten has no space to carry supplies");
            case CarryCheck.Unwell:
                throw new InvalidOperationException ("The kitten is too weak to carry supplies");
            case CarryCheck.NotStorage:
                throw new InvalidOperationException ("The habitat does not store supplies");
            case CarryCheck.OutOfReach:
                throw new InvalidOperationException ("The habitat is out of reach");
            }
            if (meals == 0 && canisters == 0)
                return;
            var before = (CarriedMeals, CarriedCanisters);
            // As the game's carry window takes and stows supplies
            InputEvents.KittenCarryBuffer.Add (new InputEvents.KittenCarryData {
                Kitten = eva,
                Host = host,
                Habitat = module,
                Direction = direction,
                Load = new CarryLoad (meals, canisters)
            });
            Waits.Until (() => (CarriedMeals, CarriedCanisters) != before);
        }

        static void WaitFor (Func<bool> condition, string error, int updates)
        {
            if (condition ())
                return;
            if (updates == 0)
                throw new InvalidOperationException (error);
            throw new YieldException<Action> (() => WaitFor (condition, error, updates - 1));
        }

        static T WaitFor<T> (Func<bool> condition, Func<T> result, string error, int updates)
        {
            if (condition ())
                return result ();
            if (updates == 0)
                throw new InvalidOperationException (error);
            throw new YieldException<Func<T>> (() => WaitFor (condition, result, error, updates - 1));
        }

        /// <summary>
        /// How the kitten on EVA is moving. Throws an exception if the kitten is not on EVA.
        /// </summary>
        [KRPCProperty]
        public LocomotionMode LocomotionMode {
            // The service enum lists the game's modes in the game's order
            get { return (LocomotionMode)EvaOrThrow.LocomotionState.Mode; }
        }

        /// <summary>
        /// The position of the kitten, in meters. For a kitten in a seat, the position of the
        /// seat's part. Throws an exception if the kitten is neither in a seat nor on EVA.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Position (ReferenceFrame referenceFrame)
        {
            var eva = Eva;
            if (eva == null)
                return SeatOrThrow.Part.Position (referenceFrame);
            return Vectors.ToTuple (referenceFrame.PositionFromEcl (eva.GetPositionEcl ()));
        }

        /// <summary>
        /// The velocity of the kitten, in meters per second. For a kitten in a seat, the
        /// velocity of the seat's part. Throws an exception if the kitten is neither in a seat
        /// nor on EVA.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the velocity.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Velocity (ReferenceFrame referenceFrame)
        {
            var eva = Eva;
            if (eva == null)
                return SeatOrThrow.Part.Velocity (referenceFrame);
            return Vectors.ToTuple (referenceFrame.VelocityFromEcl (eva.GetPositionEcl (), eva.GetVelocityEcl ()));
        }

        Seat SeatOrThrow {
            get { return Seat ?? throw new InvalidOperationException ("The kitten is neither in a seat nor on EVA"); }
        }
    }
}
