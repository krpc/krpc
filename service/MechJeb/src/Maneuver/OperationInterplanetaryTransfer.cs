using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Transfer to another planet.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationInterplanetaryTransfer : Operation
    {
        static Field<object, bool> waitForPhaseAngle;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationInterplanetaryTransfer");
            waitForPhaseAngle = type.Field<object, bool> ("WaitForPhaseAngle");
        }

        internal OperationInterplanetaryTransfer () : base ("OperationInterplanetaryTransfer")
        {
        }

        /// <summary>
        /// Whether to wait for the optimal phase angle. Otherwise the transfer happens now.
        /// </summary>
        [KRPCProperty]
        public bool WaitForPhaseAngle {
            get { return waitForPhaseAngle.Get (Instance); }
            set { waitForPhaseAngle.Set (Instance, value); }
        }
    }
}
