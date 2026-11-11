using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change the longitude of the ascending node.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationLan : TimedOperation
    {
        static Func<object, object> timeSelector;

        internal static new void Load ()
        {
            timeSelector = TimeSelectorField (Reflection.Type ("MuMech.OperationLan"));
        }

        internal OperationLan () : base ("OperationLan")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }

        /// <summary>
        /// The new longitude of the ascending node, in degrees.
        /// </summary>
        /// <remarks>
        /// MechJeb holds this value in the target controller of each vessel, as the longitude of
        /// the position target. Setting it also moves the position target.
        /// </remarks>
        [KRPCProperty]
        public double NewLAN {
            get { return MechJeb.TargetController.TargetLongitude; }
            set { MechJeb.TargetController.TargetLongitude = value; }
        }
    }
}
