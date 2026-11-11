using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// A bi-impulsive (Hohmann) transfer to the target, within one sphere of influence. It
    /// suits a rendezvous with a vessel or a transfer to a moon.
    /// </summary>
    /// <remarks>
    /// Despite the name, the transfer is often uni-impulsive. It happens at the selected time,
    /// or at the optimum time.
    /// </remarks>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationTransfer : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Value<bool> capture;
        static Value<bool> coplanar;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.OperationGeneric");
            timeSelector = TimeSelectorField (type);
            capture = Reflection.Field<bool> (type, "Capture");
            coplanar = Reflection.Field<bool> (type, "Coplanar");
        }

        internal OperationTransfer () : base ("OperationGeneric")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }

        /// <summary>
        /// Whether to plan only the intercept, with no capture burn, for an impact or a flyby.
        /// </summary>
        [KRPCProperty]
        public bool InterceptOnly {
            get { return !capture.Get (Instance); }
            set { capture.Set (Instance, !value); }
        }

        /// <summary>
        /// Whether the transfer is coplanar.
        /// </summary>
        [KRPCProperty]
        public bool SimpleTransfer {
            get { return coplanar.Get (Instance); }
            set { coplanar.Set (Instance, value); }
        }
    }
}
