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
        public double MissionTime {
            get {
                var entry = Entry;
                var total = ((UniverseTime)entry.TotalMissionTime).Seconds ();
                // The start time is set once the kitten's vehicle launches
                if (entry.AssignedToVehicle && entry.CurrentMissionStartTime.IsValid ())
                    total += (Universe.GetElapsedTime () - entry.CurrentMissionStartTime).Seconds ();
                return total;
            }
        }

        /// <summary>
        /// The distance the kitten has traveled on missions, in meters.
        /// </summary>
        [KRPCProperty]
        public double DistanceTraveled {
            get { return Entry.TravelledMeters.InMeters (); }
        }

        /// <summary>
        /// The kitten's highest speed on a mission, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double FastestSpeed {
            // The game stores the speed in a distance field
            get { return Entry.FastestSpeed.InMeters (); }
        }

        /// <summary>
        /// The kitten's acute radiation dose, in sieverts. It heals over time, and causes
        /// radiation sickness from 1 sievert and death from 4 sieverts.
        /// </summary>
        [KRPCProperty]
        public double AcuteDose {
            get { return Entry.GetAcuteDose (Universe.GetElapsedTime ()).Value (); }
        }

        /// <summary>
        /// The kitten's lifetime radiation dose, in sieverts. The kitten retires after a
        /// mission that takes it past 0.6 sieverts.
        /// </summary>
        [KRPCProperty]
        public double LifetimeDose {
            get { return Entry.LifetimeDose.Value (); }
        }

        /// <summary>
        /// The radiation dose rate the kitten receives, in sieverts per second. The vehicle's
        /// shielding reduces it in a seat, and on EVA the kitten is unshielded. Zero when the
        /// kitten is not on a mission.
        /// </summary>
        [KRPCProperty]
        public double DoseRate {
            get { return Entry.DoseRate.Value (); }
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
        public double TimeUntilFitToFly {
            get { return Entry.GetTimeUntilFitToFly (Universe.GetElapsedTime ()); }
        }

        /// <summary>
        /// The time until the kitten's acute radiation dose has healed below 1 millisievert,
        /// resting at home, in seconds.
        /// </summary>
        [KRPCProperty]
        public double RecoveryTime {
            get { return Entry.GetRecoveryTime (Universe.GetElapsedTime ()); }
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
            if (!EVADoor.PerformEvaForSeat (seat.Part.InternalVehicle, seat.InternalSeat))
                throw new InvalidOperationException ("The vehicle has no EVA hatch");
            WaitFor (() => OnEva, "The kitten did not go on EVA", 10);
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
            if (!eva.TryFindBoardTarget (out var host, out var door))
                throw new InvalidOperationException ("No EVA hatch with a free seat is in range");
            InputEvents.KittenBoardBuffer.Add (new InputEvents.KittenBoardData { Kitten = eva, Host = host, Door = door });
            return WaitFor (() => !OnEva, () => new Vehicle (host), "The kitten did not board the vehicle", 10);
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
