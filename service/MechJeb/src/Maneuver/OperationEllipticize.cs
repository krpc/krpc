using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change both the periapsis and the apoapsis.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationEllipticize : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Field<object, double> newApoapsis;
        static Field<object, double> newPeriapsis;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationEllipticize");
            timeSelector = TimeSelectorField (type);
            newApoapsis = Bindings.EditableDouble (type, "NewApA");
            newPeriapsis = Bindings.EditableDouble (type, "NewPeA");
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
