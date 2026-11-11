using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Match velocities with the target.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationKillRelVel : TimedOperation
    {
        static Func<object, object> timeSelector;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.OperationKillRelVel");
            timeSelector = TimeSelectorField (type);
        }

        internal OperationKillRelVel () : base ("OperationKillRelVel")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }
    }
}
