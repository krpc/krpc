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

    public static class TestFlightExtensions {
        // [docs]
        [KRPCProperty]
        public static bool GetFailed (this Parts parts)
        {
            throw new NotImplementedException ();
        }

        [KRPCProperty]
        public static void SetFailed (this Parts parts, bool value)
        {
            // ...
        }
        // [/docs]
    }
}
