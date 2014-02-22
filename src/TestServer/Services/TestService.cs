using KRPC.Service;

namespace TestServer.Services
{
    [KRPCService]
    public static class TestService
    {
        [KRPCProcedure]
        public static string Int32ToString (int value)
        {
            return value.ToString ();
        }
    }
}

