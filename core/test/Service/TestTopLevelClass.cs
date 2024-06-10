using KRPC.Service.Attributes;

namespace KRPC.Test.Service
{
    /// <summary>
    /// A class defined at the top level, but included in a service
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public class TestTopLevelClass
    {
        [KRPCMethod]
        public string AMethod (int x)
        {
            return x.ToString ();
        }

        [KRPCProperty]
        public string AProperty { get; set; }
    }
}
