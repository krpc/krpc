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
    [KRPCStruct (Service = "Comms")]
    public struct CommNodeInfo {
        [KRPCProperty]
        public string Name { get; set; }

        [KRPCProperty (Nullable = true)]
        public Vessel Vessel { get; set; }

        [KRPCProperty]
        public double? SignalStrength { get; set; }
    }
    // [/docs]
}
