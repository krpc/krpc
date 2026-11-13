using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// The time of a maneuver.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class TimeSelector
    {
        static Func<object, Array> allowedTimeRef;
        static Field<object, int> currentTimeRef;
        static Field<object, double> leadTime;
        static Field<object, double> circularizeAltitude;

        internal static void Load ()
        {
            var type = Bindings.Type ("MuMech.TimeSelector");
            allowedTimeRef = type.Getter<object, Array> ("_allowedTimeRef");
            currentTimeRef = type.Field<object, int> ("_currentTimeRef");
            leadTime = Bindings.EditableDouble (type, "LeadTime");
            circularizeAltitude = Bindings.EditableDouble (type, "CircularizeAltitude");
        }

        readonly TimedOperation operation;

        internal TimeSelector (TimedOperation timedOperation)
        {
            operation = timedOperation;
        }

        object Instance {
            get { return operation.TimeSelectorInstance; }
        }

        /// <summary>
        /// The time reference. Throws an exception if set to a reference the operation does
        /// not allow.
        /// </summary>
        [KRPCProperty]
        public TimeReference TimeReference {
            get {
                var instance = Instance;
                return (TimeReference)(int)allowedTimeRef (instance).GetValue (currentTimeRef.Get (instance));
            }
            set {
                var instance = Instance;
                var allowed = allowedTimeRef (instance);
                for (int i = 0; i < allowed.Length; i++) {
                    if ((int)allowed.GetValue (i) == (int)value) {
                        currentTimeRef.Set (instance, i);
                        return;
                    }
                }
                throw new OperationException ("This TimeReference is not allowed: " + value);
            }
        }

        /// <summary>
        /// The time from now, in seconds.
        /// </summary>
        /// <remarks>Applies to <see cref="TimeReference.XFromNow"/>.</remarks>
        [KRPCProperty]
        public double LeadTime {
            get { return leadTime.Get (Instance); }
            set { leadTime.Set (Instance, value); }
        }

        /// <summary>
        /// The altitude, in meters.
        /// </summary>
        /// <remarks>Applies to <see cref="TimeReference.Altitude"/>.</remarks>
        [KRPCProperty]
        public double CircularizeAltitude {
            get { return circularizeAltitude.Get (Instance); }
            set { circularizeAltitude.Set (Instance, value); }
        }
    }

    /// <summary>
    /// The time reference of a maneuver. See <see cref="TimeSelector.TimeReference"/>.
    /// </summary>
    [KRPCEnum (Service = "MechJeb")]
    public enum TimeReference
    {
        /// <summary>
        /// At the optimum time.
        /// </summary>
        Computed,

        /// <summary>
        /// After <see cref="TimeSelector.LeadTime"/>.
        /// </summary>
        XFromNow,

        /// <summary>
        /// At the next apoapsis.
        /// </summary>
        Apoapsis,

        /// <summary>
        /// At the next periapsis.
        /// </summary>
        Periapsis,

        /// <summary>
        /// At <see cref="TimeSelector.CircularizeAltitude"/>.
        /// </summary>
        Altitude,

        /// <summary>
        /// At the equatorial ascending node.
        /// </summary>
        EqAscending,

        /// <summary>
        /// At the equatorial descending node.
        /// </summary>
        EqDescending,

        /// <summary>
        /// At the next ascending node with the target.
        /// </summary>
        RelAscending,

        /// <summary>
        /// At the next descending node with the target.
        /// </summary>
        RelDescending,

        /// <summary>
        /// At the closest approach to the target.
        /// </summary>
        ClosestApproach,

        /// <summary>
        /// At the cheaper of the equatorial ascending and descending nodes.
        /// </summary>
        EqHighestAd,

        /// <summary>
        /// At the nearer of the equatorial ascending and descending nodes.
        /// </summary>
        EqNearestAd,

        /// <summary>
        /// At the cheaper of the ascending and descending nodes with the target.
        /// </summary>
        RelHighestAd,

        /// <summary>
        /// At the nearer of the ascending and descending nodes with the target.
        /// </summary>
        RelNearestAd
    }
}
