using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Util
{
    /// <summary>
    /// Holds the directions of Smart A.S.S.
    /// </summary>
    public static class Vector6
    {
        /// <summary>
        /// A direction of Smart A.S.S. See <see cref="SmartASS.AdvancedDirection"/>.
        /// </summary>
        [KRPCEnum (Service = "MechJeb")]
        public enum Direction
        {
            /// <summary>
            /// Forward.
            /// </summary>
            Forward,

            /// <summary>
            /// Back.
            /// </summary>
            Back,

            /// <summary>
            /// Up.
            /// </summary>
            Up,

            /// <summary>
            /// Down.
            /// </summary>
            Down,

            /// <summary>
            /// Right.
            /// </summary>
            Right,

            /// <summary>
            /// Left.
            /// </summary>
            Left
        }
    }
}
