using System;
using System.Linq;
using KRPC.KerbalAlarmClock.ExtensionMethods;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;
using ObjectNotLoadedException = KRPC.Service.KRPC.ObjectNotLoadedException;

namespace KRPC.KerbalAlarmClock
{
    /// <summary>
    /// Represents an alarm. Obtained by calling
    /// <see cref="KerbalAlarmClock.Alarms"/>,
    /// <see cref="KerbalAlarmClock.AlarmWithName"/> or
    /// <see cref="KerbalAlarmClock.AlarmsWithType"/>.
    /// </summary>
    [KRPCClass (Service = "KerbalAlarmClock")]
    public class Alarm : Equatable<Alarm>, IGameObjectState
    {
        // The identifier the mod creates and deletes alarms with. The alarm object the mod hands
        // out is built afresh every time the alarms are listed, and belongs to the game state it
        // was listed in, so the object itself is not held
        readonly string id;

        internal Alarm (object innerAlarm)
        {
            if (innerAlarm == null)
                throw new ArgumentNullException (nameof (innerAlarm));
            id = API.Alarm.ID (innerAlarm);
        }

        /// <summary>
        /// Check if two alarms are equal. Compares the underlying alarm
        /// identifiers, so two objects referring to the same alarm are equal.
        /// </summary>
        public override bool Equals (Alarm other)
        {
            return !ReferenceEquals (other, null) && id == other.id;
        }

        /// <summary>
        /// Hash the alarm.
        /// </summary>
        public override int GetHashCode ()
        {
            return id.GetHashCode ();
        }

        /// <summary>
        /// The state of the alarm. It is live while Kerbal Alarm Clock lists an alarm with
        /// the identifier, and destroyed once a running mod in a settled game lists none. A
        /// mod that has yet to start, or a game between states, leaves the alarm dormant.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                if (!ModAPI.Get<API> ().IsAvailable || API.WhyNotReady () != null)
                    return GameObjectState.Dormant;
                if (Find () != null)
                    return GameObjectState.Live;
                return KRPC.Service.GameState.Settled ? GameObjectState.Destroyed : GameObjectState.Dormant;
            }
        }

        /// <summary>
        /// The alarm the mod has under the identifier, or null if it has none.
        /// </summary>
        object Find ()
        {
            return ModAPI.Get<API> ().IsAvailable ? API.FindAlarm (id) : null;
        }

        /// <summary>
        /// The alarm the mod has now. Every member reaches the mod through this, so an
        /// alarm rebuilt by a game being loaded is read in place of the one this was built
        /// from.
        /// </summary>
        object Internal {
            get {
                var found = Find ();
                if (found == null)
                    throw NotResolvable ();
                return found;
            }
        }

        /// <summary>
        /// The error to raise when the mod has no alarm with the identifier, which
        /// <see cref="GameObjectState" /> decides between.
        /// </summary>
        Exception NotResolvable ()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                return new ObjectDestroyedException (
                    "The alarm no longer exists, as Kerbal Alarm Clock no longer has an " +
                    "alarm with its id.");
            return new ObjectNotLoadedException (
                "The alarm is not available, as Kerbal Alarm Clock is not running. " +
                "It can be used again once it is.");
        }

        /// <summary>
        /// The action that the alarm triggers.
        /// </summary>
        /// <remarks>
        /// An alarm that plays a sound has the action <see cref="AlarmAction.Custom"/>.
        /// See <see cref="PlaySound"/>.
        /// </remarks>
        [KRPCProperty]
        public AlarmAction Action {
            get { return API.Alarm.Action.Get (Internal).ToAlarmAction (); }
            set { API.Alarm.Action.Set (Internal, value.FromAlarmAction ()); }
        }

        /// <summary>
        /// The number of seconds before the event that the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Margin {
            get { return API.Alarm.Margin.Get (Internal); }
            set { API.Alarm.Margin.Set (Internal, value); }
        }

        /// <summary>
        /// The time at which the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Time {
            get { return API.Alarm.Time.Get (Internal); }
            set { API.Alarm.Time.Set (Internal, value); }
        }

        /// <summary>
        /// The type of the alarm.
        /// </summary>
        [KRPCProperty]
        public AlarmType Type {
            get { return API.Alarm.Type (Internal).ToAlarmType (); }
        }

        /// <summary>
        /// The unique identifier for the alarm.
        /// </summary>
        [KRPCProperty]
        public string ID {
            get { return API.Alarm.ID (Internal); }
        }

        /// <summary>
        /// The short name of the alarm.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return API.Alarm.Name.Get (Internal); }
            set { API.Alarm.Name.Set (Internal, value); }
        }

        /// <summary>
        /// The long description of the alarm.
        /// </summary>
        [KRPCProperty]
        public string Notes {
            get { return API.Alarm.Notes.Get (Internal); }
            set { API.Alarm.Notes.Set (Internal, value); }
        }

        /// <summary>
        /// The number of seconds until the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Remaining {
            // Computed from the alarm time. The mod stores its remaining time as a KSPTimeSpan
            // object
            get { return API.Alarm.Time.Get (Internal) - Planetarium.GetUniversalTime (); }
        }

        /// <summary>
        /// Whether the alarm is enabled. A disabled alarm does not fire.
        /// </summary>
        [KRPCProperty]
        public bool Enabled {
            get { return API.Alarm.Enabled.Get (Internal); }
            set { API.Alarm.Enabled.Set (Internal, value); }
        }

        /// <summary>
        /// Whether the alarm plays a sound when it fires.
        /// </summary>
        /// <remarks>
        /// An alarm that plays a sound has the action <see cref="AlarmAction.Custom"/>.
        /// Setting <see cref="Action"/> to any other value turns the sound off.
        /// </remarks>
        [KRPCProperty]
        public bool PlaySound {
            get { return API.Alarm.PlaySound.Get (API.Alarm.Actions (Internal)); }
            set { API.Alarm.PlaySound.Set (API.Alarm.Actions (Internal), value); }
        }

        /// <summary>
        /// Whether the alarm has fired. Remains true once the alarm has fired;
        /// stream this or use it in an event expression to react to the alarm
        /// firing.
        /// </summary>
        [KRPCProperty]
        public bool Triggered {
            get { return API.Alarm.Triggered (Internal); }
        }

        /// <summary>
        /// Whether the alarm will be repeated after it has fired.
        /// Only has an effect for alarm types that support repeating
        /// (see <see cref="SupportsRepeat"/>).
        /// </summary>
        [KRPCProperty]
        public bool Repeat {
            get { return API.Alarm.Repeat.Get (Internal); }
            set { API.Alarm.Repeat.Set (Internal, value); }
        }

        /// <summary>
        /// Whether this alarm's type supports repeating
        /// (see <see cref="Repeat"/>).
        /// </summary>
        [KRPCProperty]
        public bool SupportsRepeat {
            get { return API.Alarm.SupportsRepeat (Internal); }
        }

        /// <summary>
        /// The time delay to automatically create an alarm after it has fired.
        /// Only has an effect for alarm types that support a repeat period
        /// (see <see cref="SupportsRepeatPeriod"/>).
        /// </summary>
        [KRPCProperty]
        public double RepeatPeriod {
            get {
                try {
                    return API.Alarm.RepeatPeriod.Get (Internal);
                } catch (NullReferenceException) {
                    // An alarm with no repeat period
                    return 0;
                }
            }
            set { API.Alarm.RepeatPeriod.Set (Internal, value); }
        }

        /// <summary>
        /// Whether this alarm's type supports a repeat period
        /// (see <see cref="RepeatPeriod"/>).
        /// </summary>
        [KRPCProperty]
        public bool SupportsRepeatPeriod {
            get { return API.Alarm.SupportsRepeatPeriod (Internal); }
        }

        /// <summary>
        /// The vessel that the alarm is attached to.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Vessel Vessel {
            get {
                var vesselId = API.Alarm.VesselID.Get (Internal);
                var vessel = FlightGlobals.Vessels.First (x => x.id.ToString () == vesselId);
                return new SpaceCenter.Services.Vessel (vessel);
            }
            set {
                API.Alarm.VesselID.Set (Internal, value.Id.ToString ());
            }
        }

        /// <summary>
        /// The celestial body the vessel is departing from.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.CelestialBody XferOriginBody {
            get {
                var bodyName = API.Alarm.XferOriginBodyName.Get (Internal);
                var body = FlightGlobals.Bodies.First (x => bodyName == x.bodyName);
                return new SpaceCenter.Services.CelestialBody (body);
            }
            set {
                API.Alarm.XferOriginBodyName.Set (Internal, value.InternalBody.bodyName);
            }
        }

        /// <summary>
        /// The celestial body the vessel is arriving at.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.CelestialBody XferTargetBody {
            get {
                var bodyName = API.Alarm.XferTargetBodyName.Get (Internal);
                var body = FlightGlobals.Bodies.First (x => bodyName == x.bodyName);
                return new SpaceCenter.Services.CelestialBody (body);
            }
            set {
                API.Alarm.XferTargetBodyName.Set (Internal, value.InternalBody.bodyName);
            }
        }

        /// <summary>
        /// Removes the alarm. Any further use of this object throws an exception.
        /// </summary>
        /// <remarks>
        /// Any further use of this object throws an exception.
        /// </remarks>
        [KRPCMethod]
        public void Remove ()
        {
            API.DeleteAlarm (API.Alarm.ID (Internal));
        }
    }
}
