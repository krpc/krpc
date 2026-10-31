using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    // [docs]
    [KRPCClass (Service = "EVA")]
    public class Flag {
        [KRPCProperty]
        public string Name { get; set; }

        [KRPCProperty]
        public string Description { get; set; }
    }
    // [/docs]
}
