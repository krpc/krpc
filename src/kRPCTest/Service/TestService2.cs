using KRPC.Service.Attributes;

namespace KRPCTest.Service
{
    [KRPCService]
    public static class TestService2
    {
        [KRPCProcedure]
        public static int ClassTypeFromOtherServiceAsParameter (TestService.TestClass obj)
        {
            return obj.IntProperty;
        }

        [KRPCProcedure]
        public static TestService.TestClass ClassTypeFromOtherServiceAsReturn (string value)
        {
            return new TestService.TestClass (value);
        }
    }
}

