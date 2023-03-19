using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC class.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class)]
    public sealed class KRPCClassAttribute : Attribute
    {
        /// <summary>
        /// Name of the service in which the class is declared.
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// Game scene(s) in which the class' members are available.
        /// </summary>
        public GameScene GameScene { get; set; }

        /// <summary>
        /// A kRPC class.
        /// </summary>
        public KRPCClassAttribute ()
        {
            GameScene = GameScene.Inherit;
        }
    }
}
