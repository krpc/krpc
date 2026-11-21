using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A kitten's most urgent coming event: its supplies running low, or its condition
    /// worsening. Obtained from <see cref="Vehicle.CrewEvents"/> or
    /// <see cref="Vehicle.NextCrewEvent"/>.
    /// </summary>
    /// <remarks>
    /// As the game's crew clock, an event within a day is the worst stage the kitten reaches
    /// in that time, and a later event is the soonest. The event is the game's forecast when it
    /// is read from the vehicle. Read it again for a new forecast.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class CrewEvent : Equatable<CrewEvent>, IGameObjectState
    {
        readonly Vehicle vehicle;

        CrewEvent (Vehicle vehicle, global::KSA.CrewEvent crewEvent, UniverseTime now)
        {
            this.vehicle = vehicle;
            Kitten = new Kitten (crewEvent.Kitten.Name);
            Stage = (CrewStage)crewEvent.Stage;
            Hazard = (CrewHazard)crewEvent.Hazard;
            UT = (now + crewEvent.Remaining).Seconds ();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (CrewEvent other)
        {
            return !ReferenceEquals (other, null) && vehicle.Equals (other.vehicle) && Kitten.Equals (other.Kitten) &&
                Stage == other.Stage && Hazard == other.Hazard && UT.Equals (other.UT);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return vehicle.GameObjectState; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return HashCode.Combine (vehicle, Kitten, Stage, Hazard, UT);
        }

        // Scratch space for the game's crew events
        static readonly List<global::KSA.CrewEvent> events = new List<global::KSA.CrewEvent> ();

        // The next event of each kitten in the vehicle, most urgent first, as the game's crew
        // clock orders them
        internal static List<CrewEvent> Of (Vehicle vehicle)
        {
            events.Clear ();
            vehicle.InternalVehicle.AddCrewEvents (events);
            events.Sort (CrewCountdown.Comparison);
            var now = Universe.GetElapsedTime ();
            var result = new List<CrewEvent> (events.Count);
            foreach (var crewEvent in events)
                result.Add (new CrewEvent (vehicle, crewEvent, now));
            return result;
        }

        /// <summary>
        /// The kitten.
        /// </summary>
        [KRPCProperty]
        public Kitten Kitten { get; }

        /// <summary>
        /// What happens to the kitten.
        /// </summary>
        [KRPCProperty]
        public CrewStage Stage { get; }

        /// <summary>
        /// The cause of the event.
        /// </summary>
        [KRPCProperty]
        public CrewHazard Hazard { get; }

        /// <summary>
        /// The universal time of the event, in seconds.
        /// </summary>
        [KRPCProperty]
        public double UT { get; }

        /// <summary>
        /// The time until the event, in seconds. Zero once its time has passed.
        /// </summary>
        [KRPCProperty]
        public float TimeTo {
            get { return (float)Math.Max (0, UT - Universe.GetElapsedSeconds ()); }
        }
    }
}
