using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC method.
    /// </summary>
    [AttributeUsage (AttributeTargets.Method)]
    public sealed class KRPCMethodAttribute : Attribute
    {
        /// <summary>
        /// Whether the return value can be null.
        /// </summary>
        public bool Nullable { get; set; }

        /// <summary>
        /// Game scene(s) in which the method is available.
        /// </summary>
        public GameScene GameScene { get; set; }

        /// <summary>
        /// A kRPC method.
        /// </summary>
        public KRPCMethodAttribute ()
        {
            GameScene = GameScene.Inherit;
        }
    }
}
