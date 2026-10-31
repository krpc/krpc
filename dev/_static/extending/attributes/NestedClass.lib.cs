using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    // [docs]
    [KRPCService]
    public static class EVA {
        [KRPCClass]
        public class Flag {
            // ...
        }
    }
    // [/docs]
}
