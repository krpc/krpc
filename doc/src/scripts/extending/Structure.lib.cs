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
    [KRPCStruct (Service = "EVA")]
    public struct LandingSite {
        [KRPCProperty]
        public string Name { get; set; }

        [KRPCProperty]
        public CelestialBody Body { get; set; }

        [KRPCProperty]
        public double Latitude { get; set; }
    }
    // [/docs]

    [KRPCService]
    public static class EVA {
        [KRPCProperty]
        public static LandingSite LandingSite
        {
            get { throw new NotImplementedException (); }
        }
    }
}
