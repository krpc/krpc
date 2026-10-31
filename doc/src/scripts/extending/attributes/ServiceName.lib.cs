using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    // [docs]
    [KRPCService (Name = "MyEVAService")]
    public static class EVA {
        // ...
    }
    // [/docs]
}
