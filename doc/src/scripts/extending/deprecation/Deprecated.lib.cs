using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    [KRPCService]
    public static class EVA {
        // [docs]
        [KRPCProcedure]
        [Obsolete ("Use <see cref='Flag.Remove'/> instead.")]
        public static void RemoveFlag(Flag flag) {
            // ...
        }
        // [/docs]
    }

    [KRPCClass (Service = "EVA")]
    public class Flag {
        [KRPCMethod]
        public void Remove ()
        {
        }
    }
}
