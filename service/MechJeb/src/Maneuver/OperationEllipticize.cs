using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change both the periapsis and the apoapsis.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationEllipticize : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Value<double> newApoapsis;
        static Value<double> newPeriapsis;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.OperationEllipticize");
            timeSelector = TimeSelectorField (type);
            newApoapsis = Reflection.EditableDouble (type, "NewApA");
            newPeriapsis = Reflection.EditableDouble (type, "NewPeA");
        }

        internal OperationEllipticize () : base ("OperationEllipticize")
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
