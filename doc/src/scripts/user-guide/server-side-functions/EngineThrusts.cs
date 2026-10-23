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
        var engines = vessel.Parts.Engines;
        var stream = connection.AddStream (
            () => engines.Select (engine => engine.Thrust).ToList ());
        foreach (var thrust in stream.Get ())
            Console.WriteLine (thrust);
        // [/docs]
    }
}
