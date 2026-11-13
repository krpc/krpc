using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change the periapsis.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationPeriapsis : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Field<object, double> newPeriapsis;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationPeriapsis");
            timeSelector = TimeSelectorField (type);
            newPeriapsis = Bindings.EditableDouble (type, "NewPeA");
        }

        internal OperationPeriapsis () : base ("OperationPeriapsis")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }

        /// <summary>
        /// The new periapsis altitude, in meters.
        /// </summary>
        [KRPCProperty]
        public double NewPeriapsis {
            get { return newPeriapsis.Get (Instance); }
            set { newPeriapsis.Set (Instance, value); }
        }
    }
}
