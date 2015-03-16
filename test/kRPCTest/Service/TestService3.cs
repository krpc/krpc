using KRPC.Service.Attributes;
using KRPC.Service;

namespace KRPCTest.Service
{
    [KRPCService (Name = "TestService3Name", GameScene = GameScene.Editor)]
    public static class TestService3
    {
        [KRPCProcedure]
        public static void AProcedure ()
        {
        }
    }

    [KRPCClass (Service = "TestService3Name")]
    public class TestClass3
    {
    }
}
