using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC service.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class)]
    public sealed class KRPCServiceAttribute : Attribute
    {
        /// <summary>
        /// Name of the service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Id of the service. Optional.
        /// </summary>
        public uint Id { get; set; }

        /// <summary>
        /// Game scene(s) in which the service is available.
        /// </summary>
        public GameScene GameScene { get; set; }

        /// <summary>
        /// A kRPC service.
        /// </summary>
        public KRPCServiceAttribute ()
        {
            GameScene = GameScene.All;
        }
    }
}
