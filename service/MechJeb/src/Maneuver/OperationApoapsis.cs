using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change the apoapsis.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationApoapsis : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Value<double> newApoapsis;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.OperationApoapsis");
            timeSelector = TimeSelectorField (type);
            newApoapsis = Reflection.EditableDouble (type, "NewApA");
        }

        internal OperationApoapsis () : base ("OperationApoapsis")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }

        /// <summary>
        /// The new apoapsis altitude, in meters.
        /// </summary>
        [KRPCProperty]
        public double NewApoapsis {
            get { return newApoapsis.Get (Instance); }
            set { newApoapsis.Set (Instance, value); }
        }
    }
}
