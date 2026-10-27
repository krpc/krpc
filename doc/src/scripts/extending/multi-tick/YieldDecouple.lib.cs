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

    [KRPCClass (Service = "Examples")]
    public class Decoupler {
        readonly ModuleDecouple decoupler = null;
        bool Decoupled { get { return decoupler.isDecoupled; } }

        // [docs]
        [KRPCMethod]
        public Vessel Decouple()
        {
            var preVesselIds = FlightGlobals.Vessels.Select(v => v.id).ToList();
            decoupler.Decouple();
            return PostDecouple(preVesselIds);
        }

        Vessel PostDecouple(IList<Guid> preVesselIds, int wait = 0)
        {
            if (wait < 10 || !Decoupled)
                throw new YieldException<Func<Vessel>>(() => PostDecouple(preVesselIds, wait + 1));
            return new Vessel(FlightGlobals.Vessels.Select(v => v.id).Except(preVesselIds).First());
        }
        // [/docs]
    }
}
