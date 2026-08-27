using KRPC.Service.Attributes;

namespace TestServer
{
    /// <summary>
    /// A second service. An extension member of TestService returns its class.
    /// </summary>
    [KRPCService (Id = 9997)]
    public static class TestService2
    {
        /// <summary>
        /// Class documentation string.
        /// </summary>
        [KRPCClass]
        public sealed class TestClass2
        {
            readonly string value;

            public TestClass2 (string value)
            {
                this.value = value;
            }

            /// <summary>
            /// Property documentation string.
            /// </summary>
            [KRPCProperty]
            public string Value {
                get { return value; }
            }
        }
    }
}
