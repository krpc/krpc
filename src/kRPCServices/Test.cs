using System;
using Google.ProtocolBuffers;
using KRPC.Utils;
using KRPC.Service;

namespace KRPCServices
{
    [KRPCService]
    public class Test
    {
        [KRPCProcedure]
        public static KRPC.Schema.Test.Echo Echo (KRPC.Schema.Test.Echo request)
        {
            return request;
        }
    }
}
