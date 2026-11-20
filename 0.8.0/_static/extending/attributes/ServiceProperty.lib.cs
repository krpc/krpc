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
        [KRPCProperty]
        public static Flag LastFlag
        {
            get { throw new NotImplementedException (); }
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
