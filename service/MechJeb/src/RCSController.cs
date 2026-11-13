using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The RCS controller.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class RCSController : ComputerModule
    {
        static Field<object, bool> rcsThrottle;
        static Field<object, bool> rcsForRotation;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleRCSController");
            rcsThrottle = type.Field<object, bool> ("rcsThrottle");
            rcsForRotation = type.Field<object, bool> ("rcsForRotation");
        }

        internal RCSController () : base ("MechJebModuleRCSController")
        {
        }

        /// <summary>
        /// Whether the controller throttles the RCS thrusters.
        /// </summary>
        [KRPCProperty]
        public bool RCSThrottle {
            get { return rcsThrottle.Get (Instance); }
            set { rcsThrottle.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the controller uses RCS to rotate the vessel.
        /// </summary>
        [KRPCProperty]
        public bool RCSForRotation {
            get { return rcsForRotation.Get (Instance); }
            set { rcsForRotation.Set (Instance, value); }
        }
    }
}
