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
    public static class TestFlightExtensions {
        /// <summary>The parts that have the named failure.</summary>
        [KRPCMethod]
        public static IList<Part> WithFailure (this Parts parts, string failure)
        {
            throw new NotImplementedException ();
        }
    }
    // [/docs]
}
