using KRPC.Service.Attributes;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Service.StdLib
{
    /// <summary>
    /// A rotation as an axis and an angle.
    /// </summary>
    /// <remarks>
    /// See <see cref="StdLib.QuaternionToAxisAngle"/>.
    /// </remarks>
    [KRPCStruct (Service = "StdLib")]
    public struct AxisAngle
    {
        internal AxisAngle (Tuple3 axis, double angle) : this ()
        {
            Axis = axis;
            Angle = angle;
        }

        /// <summary>
        /// The axis of the rotation, as a unit vector, or zero for no rotation.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Axis { get; set; }

        /// <summary>
        /// The angle of the rotation, in radians between 0 and pi.
        /// </summary>
        [KRPCProperty]
        public double Angle { get; set; }
    }
}
