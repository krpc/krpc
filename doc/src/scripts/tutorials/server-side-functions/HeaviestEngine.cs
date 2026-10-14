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
        var heaviest = connection.Run (
            () => vessel.Parts.Engines.Count == 0 ? null
                : vessel.Parts.Engines.OrderBy (engine => engine.Part.Mass).Last ());
        // [/docs]
    }
}
