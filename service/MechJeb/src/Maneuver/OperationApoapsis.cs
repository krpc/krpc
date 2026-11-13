using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change the apoapsis.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationApoapsis : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Field<object, double> newApoapsis;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationApoapsis");
            timeSelector = TimeSelectorField (type);
            newApoapsis = Bindings.EditableDouble (type, "NewApA");
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
