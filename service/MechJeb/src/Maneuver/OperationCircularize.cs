using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Circularize the orbit.
    /// </summary>
    /// <remarks>
    /// To raise the periapsis to the apoapsis, set the time to <see
    /// cref="TimeReference.Apoapsis"/>, and to lower the apoapsis to the periapsis, set it to <see
    /// cref="TimeReference.Periapsis"/>. These are the cheapest.
    /// </remarks>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationCircularize : TimedOperation
    {
        static Func<object, object> timeSelector;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.OperationCircularize");
            timeSelector = TimeSelectorField (type);
        }

        internal OperationCircularize () : base ("OperationCircularize")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }
    }
}
