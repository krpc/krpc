using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC procedure.
    /// </summary>
    [AttributeUsage (AttributeTargets.Method)]
    public sealed class KRPCProcedureAttribute : Attribute
    {
        /// <summary>
        /// Whether the return value can be null.
        /// </summary>
        public bool Nullable { get; set; }

        /// <summary>
        /// Game scene(s) in which the procedure is available.
        /// </summary>
        public GameScene GameScene { get; set; }

        /// <summary>
        /// A kRPC procedure.
        /// </summary>
        public KRPCProcedureAttribute ()
        {
            GameScene = GameScene.Inherit;
        }
    }
}
