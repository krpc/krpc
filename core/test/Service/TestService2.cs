using KRPC.Service.Attributes;

namespace KRPC.Test.Service
{
    /// <summary>
    /// TestService2 documentation.
    /// </summary>
    [KRPCService]
    public static class TestService2
    {
        /// <summary>
        /// TestService2 procedure documentation.
        /// </summary>
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
