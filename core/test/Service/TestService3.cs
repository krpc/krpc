using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.Test.Service
{
    [KRPCService (Name = "TestService3Name", GameScene = GameScene.Editor, Id = 1234)]
    public static class TestService3
    {
        [KRPCProcedure]
        public static void AProcedure ()
        {
        }

        [KRPCProperty]
        public static int AProperty {
            get { return 0; }
        }
    }

    [KRPCClass (Service = "TestService3Name")]
    public class TestClass3
    {
    }
}
