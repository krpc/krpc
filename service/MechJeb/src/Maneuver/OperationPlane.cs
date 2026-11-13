using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Match planes with the target.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationPlane : TimedOperation
    {
        static Func<object, object> timeSelector;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationPlane");
            timeSelector = TimeSelectorField (type);
        }

        internal OperationPlane () : base ("OperationPlane")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }
    }
}
