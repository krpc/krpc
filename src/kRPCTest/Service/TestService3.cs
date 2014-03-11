using KRPC.Service.Attributes;

namespace KRPCTest.Service
{
    [KRPCService (Name = "TestService3Name")]
    public static class TestService3a
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
