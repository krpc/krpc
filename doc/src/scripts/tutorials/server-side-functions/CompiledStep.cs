using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
using Type = KRPC.Client.Services.KRPC.Type;

class Program {
    public static void Main () {
        var connection = new Connection ();
        var spaceCenter = connection.SpaceCenter ();
        var vessel = spaceCenter.ActiveVessel;
        // [docs]
        var step = connection.Compile (() => vessel.Control.ToggleActionGroup (1));
        while (true)
            connection.Run (step);
        // [/docs]
    }
}
