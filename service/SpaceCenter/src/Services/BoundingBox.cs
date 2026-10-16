using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// An axis-aligned bounding box, as the positions of its minimum and maximum vertices.
    /// </summary>
    [KRPCStruct (Service = "SpaceCenter")]
    public struct BoundingBox
    {
        internal BoundingBox (Bounds bounds) : this ()
        {
            Min = bounds.min.ToTuple ();
            Max = bounds.max.ToTuple ();
        }

        /// <summary>
        /// The position of the minimum vertex of the box.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Min { get; set; }

        /// <summary>
        /// The position of the maximum vertex of the box.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Max { get; set; }
    }
}
