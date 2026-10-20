using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A force and a torque that act together.
    /// </summary>
    [KRPCStruct (Service = "SpaceCenter")]
    public struct Wrench
    {
        internal Wrench (Vector3d force, Vector3d torque) : this ()
        {
            Force = force.ToTuple ();
            Torque = torque.ToTuple ();
        }

        /// <summary>
        /// The force, in Newtons.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Force { get; set; }

        /// <summary>
        /// The torque, in Newton meters.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Torque { get; set; }
    }
}
