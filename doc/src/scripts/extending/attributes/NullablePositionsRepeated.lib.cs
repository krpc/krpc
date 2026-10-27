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
    using static KRPC.Service.Attributes.Position;

    [KRPCService]
    public static class Fleet {
        [KRPCProcedure]
        public static void Assign (
            // [docs]
            [KRPCNullable]
            [KRPCNullable (Element)]
            IList<Vessel> maybeVessels                                        // the list, and its vessels
            // [/docs]
        )
        {
        }
    }
}
