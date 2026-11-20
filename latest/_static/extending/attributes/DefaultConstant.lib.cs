using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    [KRPCService]
    public static class Crew {
        // [docs]
        [KRPCProcedure]
        public static void HireKerbal (string name = "Jeb")
        {
            // ...
        }
        // [/docs]
    }
}
