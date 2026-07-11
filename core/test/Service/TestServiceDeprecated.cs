using System;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.Test.Service
{
    /// <summary>
    /// A deprecated service, annotated with a reason.
    /// </summary>
    [KRPCService (GameScene = GameScene.Flight)]
    [Obsolete ("Use <see cref='TestService'/> instead.")]
    public static class TestServiceDeprecated
    {
        [KRPCProcedure]
        public static void AProcedure ()
        {
        }
    }
}
