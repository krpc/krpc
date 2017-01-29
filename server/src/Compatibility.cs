using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Server;
using KRPC.Service;

namespace KRPC
{
    /// <summary>
    /// Deprecated. See <see cref="Core"/>
    /// </summary>
    [Obsolete ("use KRPC.Core")]
    public static class KRPCCore
    {
        /// <summary>
        /// Deprecated. See <see cref="CallContext"/>
        /// </summary>
        [Obsolete ("use KRPC.Service.CallContext")]
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
        public static class Context
        {
            /// <summary>
            /// The current client
            /// </summary>
            [Obsolete ("use KRPC.Service.CallContext.Client")]
            public static IClient RPCClient {
                get { return CallContext.Client; }
            }

            /// <summary>
            /// The current game scene
            /// </summary>
            [Obsolete ("use KRPC.Service.CallContext.GameScene")]
            public static GameScene GameScene {
                get { return CallContext.GameScene; }
            }
        }
    }
}
