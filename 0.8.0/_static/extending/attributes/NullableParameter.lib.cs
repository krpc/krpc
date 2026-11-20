using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    using KRPC.SpaceCenter;
    using KRPC.SpaceCenter.Services;
    using KRPC.SpaceCenter.Services.Parts;
    using KRPC.Utils;

    // [docs]
    [KRPCService]
    public static class EVA {
        [KRPCProcedure]
        public static void DestroyVessel ([KRPCNullable] Vessel vessel)
        {
            // vessel may be null
            // ...
        }
    }
    // [/docs]
}
