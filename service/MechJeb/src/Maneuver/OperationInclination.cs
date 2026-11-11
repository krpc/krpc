using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change the inclination.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationInclination : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Value<double> newInclination;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.OperationInclination");
            timeSelector = TimeSelectorField (type);
            newInclination = Reflection.EditableDouble (type, "NewInc");
        }

        internal OperationInclination () : base ("OperationInclination")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }

        /// <summary>
        /// The new inclination, in degrees.
        /// </summary>
        [KRPCProperty]
        public double NewInclination {
            get { return newInclination.Get (Instance); }
            set { newInclination.Set (Instance, value); }
        }
    }
}
