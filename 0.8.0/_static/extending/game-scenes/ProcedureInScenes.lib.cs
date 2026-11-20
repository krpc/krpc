using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    // [docs]
    [KRPCService (GameScene = GameScene.Flight)]
    public static class MyService {
        // ...

        [KRPCProcedure (GameScene = GameScene.All)]
        public static string MyProcedure() {
            throw new NotImplementedException ();
        }

        // ...
    }
    // [/docs]
}
