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
        [KRPCProcedure]
        public static Flag PlantFlag (string name, string description = "")
        {
            throw new NotImplementedException ();
        }
    }
    // [/docs]

    [KRPCClass (Service = "EVA")]
    public class Flag {
        [KRPCMethod]
        public void Remove ()
        {
        }
    }
}
