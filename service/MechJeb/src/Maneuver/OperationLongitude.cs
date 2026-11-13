using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change the surface longitude of the apsis.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationLongitude : TimedOperation
    {
        static Func<object, object> timeSelector;

        internal static new void Load ()
        {
            timeSelector = TimeSelectorField (Bindings.Type ("MuMech.OperationLongitude"));
        }

        internal OperationLongitude () : base ("OperationLongitude")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }

        /// <summary>
        /// The new surface longitude of the apsis after one orbit, in degrees.
        /// </summary>
        /// <remarks>
        /// MechJeb holds this value in the target controller of each vessel, as the longitude of
        /// the position target. Setting it also moves the position target.
        /// </remarks>
        [KRPCProperty]
        public double NewSurfaceLongitude {
            get { return MechJeb.TargetController.TargetLongitude; }
            set { MechJeb.TargetController.TargetLongitude = value; }
        }
    }
}
