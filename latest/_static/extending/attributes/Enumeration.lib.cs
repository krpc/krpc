using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    // [docs]
    [KRPCEnum (Service = "EVA")]
    public enum FlagState {
        Raised,
        Lowered
    }
    // [/docs]
}
