using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    [KRPCClass (Service = "Examples")]
    public class Launcher {
        bool IsReady { get { return true; } }

        // [docs]
        [KRPCMethod]
        public void Wait()
        {
            if (!IsReady)
                throw new YieldException<Action>(Wait);
            // proceed once the condition is satisfied
        }
        // [/docs]
    }
}
