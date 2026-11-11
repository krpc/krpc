using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change the periapsis.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationPeriapsis : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Value<double> newPeriapsis;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.OperationPeriapsis");
            timeSelector = TimeSelectorField (type);
            newPeriapsis = Reflection.EditableDouble (type, "NewPeA");
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
