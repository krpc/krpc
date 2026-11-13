using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Intercept the target at a chosen time.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationLambert : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Field<object, double> interceptInterval;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationLambert");
            timeSelector = TimeSelectorField (type);
            interceptInterval = Bindings.EditableDouble (type, "InterceptInterval");
        }

        internal OperationLambert () : base ("OperationLambert")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }

        /// <summary>
        /// The time from the maneuver to the intercept, in seconds.
        /// </summary>
        [KRPCProperty]
        public double InterceptInterval {
            get { return interceptInterval.Get (Instance); }
            set { interceptInterval.Set (Instance, value); }
        }
    }
}
