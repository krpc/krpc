using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC property.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class KRPCPropertyAttribute : Attribute
    {
        /// <summary>
        /// Whether the return value (of the getter) can be null.
        /// </summary>
        public bool Nullable { get; set; }

        /// <summary>
        /// Game scene(s) in which the property is available.
        /// </summary>
        public GameScene GameScene { get; set; }

        /// <summary>
        /// A kRPC property.
        /// </summary>
        public KRPCPropertyAttribute ()
        {
            GameScene = GameScene.Inherit;
        }
    }
}
