using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The RCS controller.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class RCSController : ComputerModule
    {
        static Value<bool> rcsThrottle;
        static Value<bool> rcsForRotation;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleRCSController");
            rcsThrottle = Reflection.Field<bool> (type, "rcsThrottle");
            rcsForRotation = Reflection.Field<bool> (type, "rcsForRotation");
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
