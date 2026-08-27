using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC property. Applied to a property, or to a GetX or SetX extension method that
    /// adds a property to another service's class.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property | AttributeTargets.Method)]
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
