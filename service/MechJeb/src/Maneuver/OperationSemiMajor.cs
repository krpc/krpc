using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change the semi-major axis.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationSemiMajor : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Field<object, double> newSemiMajorAxis;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationSemiMajor");
            timeSelector = TimeSelectorField (type);
            newSemiMajorAxis = Bindings.EditableDouble (type, "NewSma");
        }

        internal OperationSemiMajor () : base ("OperationSemiMajor")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }

        /// <summary>
        /// The new semi-major axis, in meters.
        /// </summary>
        [KRPCProperty]
        public double NewSemiMajorAxis {
            get { return newSemiMajorAxis.Get (Instance); }
            set { newSemiMajorAxis.Set (Instance, value); }
        }
    }
}
