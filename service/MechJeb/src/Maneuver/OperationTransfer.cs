using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

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
        static Field<object, bool> capture;
        static Field<object, bool> planCapture;
        static Field<object, bool> rendezvous;
        static Field<object, double> lagTime;
        static Field<object, bool> coplanar;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationGeneric");
            timeSelector = TimeSelectorField (type);
            capture = type.Field<object, bool> ("Capture");
            planCapture = type.Field<object, bool> ("PlanCapture");
            rendezvous = type.Field<object, bool> ("Rendezvous");
            lagTime = Bindings.EditableDouble (type, "LagTime");
            coplanar = type.Field<object, bool> ("Coplanar");
        }

        internal OperationTransfer () : base ("OperationGeneric")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }

        /// <summary>
        /// Whether the transfer ends in the target's orbit. Without capture, it is an impact or
        /// a flyby.
        /// </summary>
        [KRPCProperty]
        public bool Capture {
            get { return capture.Get (Instance); }
            set { capture.Set (Instance, value); }
        }

        /// <summary>
        /// Whether to make a second node for the capture burn.
        /// </summary>
        /// <remarks>Requires <see cref="Capture"/>.</remarks>
        [KRPCProperty]
        public bool PlanCapture {
            get { return planCapture.Get (Instance); }
            set { planCapture.Set (Instance, value); }
        }

        /// <summary>
        /// Whether to meet the target, rather than reach its orbit.
        /// </summary>
        [KRPCProperty]
        public bool Rendezvous {
            get { return rendezvous.Get (Instance); }
            set { rendezvous.Set (Instance, value); }
        }

        /// <summary>
        /// The time by which the vessel arrives behind the target, in seconds.
        /// </summary>
        /// <remarks>Requires <see cref="Rendezvous"/>.</remarks>
        [KRPCProperty]
        public double LagTime {
            get { return lagTime.Get (Instance); }
            set { lagTime.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the transfer stays in the vessel's orbital plane.
        /// </summary>
        [KRPCProperty]
        public bool Coplanar {
            get { return coplanar.Get (Instance); }
            set { coplanar.Set (Instance, value); }
        }
    }
}
