using KRPC.Service.Attributes;

namespace TestServer
{
    /// <summary>
    /// Members added to TestService's classes from outside TestService.
    /// </summary>
    public static class TestServiceExtensions
    {
        /// <summary>
        /// Extension method documentation string.
        /// </summary>
        [KRPCMethod]
        public static string ExtensionMethod (this TestService.TestClass obj, int x)
        {
            return obj.GetValue () + x;
        }

        [KRPCMethod]
        public static TestService2.TestClass2 ExtensionMethodReturningClassFromOtherService (
            this TestService.TestClass obj)
        {
            return new TestService2.TestClass2 (obj.GetValue ());
        }

        /// <summary>
        /// Extension property documentation string.
        /// </summary>
        [KRPCProperty]
        public static string GetExtensionProperty (this TestService.TestClass obj)
        {
            return obj.GetValue ();
        }

        [KRPCProperty]
        public static int GetExtensionReadWriteProperty (this TestService.TestClass obj)
        {
            return obj.IntProperty;
        }

        [KRPCProperty]
        public static void SetExtensionReadWriteProperty (this TestService.TestClass obj, int value)
        {
            obj.IntProperty = value;
        }
    }
}
