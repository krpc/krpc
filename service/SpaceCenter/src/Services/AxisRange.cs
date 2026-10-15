using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using Tuple3 = System.Tuple<double, double, double>;
using TupleV3 = System.Tuple<Vector3d, Vector3d>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The largest value of a quantity, such as a torque or a force, in the positive and the
    /// negative direction of each of three axes.
    /// </summary>
    [KRPCStruct (Service = "SpaceCenter")]
    public struct AxisRange
    {
        internal AxisRange (TupleV3 range) : this ()
        {
            if (range == null)
                throw new ArgumentNullException (nameof (range));
            Positive = range.Item1.ToTuple ();
            Negative = range.Item2.ToTuple ();
        }

        /// <summary>
        /// The value in the positive direction of each axis.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Positive { get; set; }

        /// <summary>
        /// The value in the negative direction of each axis, which is zero or less.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Negative { get; set; }
    }
}
