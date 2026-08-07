using System;
using System.Linq;
using KRPC.KerbalAlarmClock.ExtensionMethods;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

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
        // The identifier the mod knows the alarm by, which is what it creates and deletes
        // alarms with. The alarm object the mod hands out is built afresh every time the
        // alarms are listed, and belongs to the game state it was listed in, so holding one
        // would leave this reading an alarm the mod no longer has.
        readonly string id;

        internal Alarm (KACWrapper.KACAPI.KACAlarm innerAlarm)
        {
            if (innerAlarm == null)
                throw new ArgumentNullException (nameof (innerAlarm));
            id = innerAlarm.ID;
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
        /// What the game holds for the alarm. It is live while Kerbal Alarm Clock lists an
        /// alarm with the identifier, and destroyed once the mod is there to ask and lists
        /// none, as an alarm that is removed is gone for good. The mod not being ready says
        /// nothing about any alarm.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                if (!KACWrapper.APIReady)
                    return GameObjectState.Dormant;
                return Find () != null ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        /// <summary>
        /// The alarm the mod has under the identifier, or null if it has none.
        /// </summary>
        KACWrapper.KACAPI.KACAlarm Find ()
        {
            if (!KACWrapper.APIReady)
                return null;
            var alarms = KACWrapper.KAC.Alarms;
            for (var i = 0; i < alarms.Count; i++) {
                if (alarms [i].ID == id)
                    return alarms [i];
            }
            return null;
        }

        /// <summary>
        /// The alarm the mod has now. Every member reaches the mod through this, so an
        /// alarm rebuilt by a game being loaded is read in place of the one this was built
        /// from.
        /// </summary>
        KACWrapper.KACAPI.KACAlarm Internal {
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
            return new InvalidOperationException (
                "The alarm is not available, as Kerbal Alarm Clock is not ready. " +
                "It can be used again once it is.");
        }

        /// <summary>
        /// The action that the alarm triggers.
        /// </summary>
        [KRPCProperty]
        public AlarmAction Action {
            get { return Internal.AlarmAction.ToAlarmAction (); }
            set { Internal.AlarmAction = value.FromAlarmAction (); }
        }

        /// <summary>
        /// The number of seconds before the event that the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Margin {
            get { return Internal.AlarmMargin; }
            set { Internal.AlarmMargin = value; }
        }

        /// <summary>
        /// The time at which the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Time {
            get { return Internal.AlarmTime; }
            set { Internal.AlarmTime = value; }
        }

        /// <summary>
        /// The type of the alarm.
        /// </summary>
        [KRPCProperty]
        public AlarmType Type {
            get { return Internal.AlarmType.ToAlarmType (); }
        }

        /// <summary>
        /// The unique identifier for the alarm.
        /// </summary>
        [KRPCProperty]
        public string ID {
            get { return Internal.ID; }
        }

        /// <summary>
        /// The short name of the alarm.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return Internal.Name; }
            set { Internal.Name = value; }
        }

        /// <summary>
        /// The long description of the alarm.
        /// </summary>
        [KRPCProperty]
        public string Notes {
            get { return Internal.Notes; }
            set { Internal.Notes = value; }
        }

        /// <summary>
        /// The number of seconds until the alarm will fire.
        /// </summary>
        [KRPCProperty]
        public double Remaining {
            // Computed from the alarm time rather than read through the wrapper:
            // the mod stores its remaining time as a KSPTimeSpan object, which the
            // wrapper cannot unbox to a double.
            get { return Internal.AlarmTime - Planetarium.GetUniversalTime (); }
        }

        /// <summary>
        /// Whether the alarm is enabled. A disabled alarm does not fire.
        /// </summary>
        [KRPCProperty]
        public bool Enabled {
            get { return Internal.Enabled; }
            set { Internal.Enabled = value; }
        }

        /// <summary>
        /// Whether the alarm plays a sound when it fires.
        /// </summary>
        [KRPCProperty]
        public bool PlaySound {
            get { return Internal.PlaySound; }
            set { Internal.PlaySound = value; }
        }

        /// <summary>
        /// Whether the alarm has fired. Remains true once the alarm has fired;
        /// stream this or use it in an event expression to react to the alarm
        /// firing.
        /// </summary>
        [KRPCProperty]
        public bool Triggered {
            get { return Internal.Triggered; }
        }

        /// <summary>
        /// Whether the alarm will be repeated after it has fired.
        /// Only has an effect for alarm types that support repeating
        /// (see <see cref="SupportsRepeat"/>).
        /// </summary>
        [KRPCProperty]
        public bool Repeat {
            get { return Internal.RepeatAlarm; }
            set { Internal.RepeatAlarm = value; }
        }

        /// <summary>
        /// Whether this alarm's type supports repeating
        /// (see <see cref="Repeat"/>).
        /// </summary>
        [KRPCProperty]
        public bool SupportsRepeat {
            get { return Internal.SupportsRepeat; }
        }

        /// <summary>
        /// The time delay to automatically create an alarm after it has fired.
        /// Only has an effect for alarm types that support a repeat period
        /// (see <see cref="SupportsRepeatPeriod"/>).
        /// </summary>
        [KRPCProperty]
        public double RepeatPeriod {
            get { return Internal.RepeatAlarmPeriod; }
            set { Internal.RepeatAlarmPeriod = value; }
        }

        /// <summary>
        /// Whether this alarm's type supports a repeat period
        /// (see <see cref="RepeatPeriod"/>).
        /// </summary>
        [KRPCProperty]
        public bool SupportsRepeatPeriod {
            get { return Internal.SupportsRepeatPeriod; }
        }

        /// <summary>
        /// The vessel that the alarm is attached to.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Vessel Vessel {
            get {
                var vesselId = Internal.VesselID;
                var vessel = FlightGlobals.Vessels.First (x => x.id.ToString () == vesselId);
                return new SpaceCenter.Services.Vessel (vessel);
            }
            set {
                Internal.VesselID = value.Id.ToString ();
            }
        }

        /// <summary>
        /// The celestial body the vessel is departing from.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.CelestialBody XferOriginBody {
            get {
                var bodyName = Internal.XferOriginBodyName;
                var body = FlightGlobals.Bodies.First (x => bodyName == x.bodyName);
                return new SpaceCenter.Services.CelestialBody (body);
            }
            set {
                Internal.XferOriginBodyName = value.InternalBody.bodyName;
            }
        }

        /// <summary>
        /// The celestial body the vessel is arriving at.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.CelestialBody XferTargetBody {
            get {
                var bodyName = Internal.XferTargetBodyName;
                var body = FlightGlobals.Bodies.First (x => bodyName == x.bodyName);
                return new SpaceCenter.Services.CelestialBody (body);
            }
            set {
                Internal.XferTargetBodyName = value.InternalBody.bodyName;
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
            KACWrapper.KAC.DeleteAlarm (Internal.ID);
        }
    }
}
