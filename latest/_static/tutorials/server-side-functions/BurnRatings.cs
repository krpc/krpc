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
        // A C# lambda holds a single expression, so LINQ operators replace the loop
        var ratings = connection.Run (
            () => vessel.Parts.Engines
                .Where (engine => engine.Active)
                .Select (engine =>
                    engine.Thrust > 0.9 * engine.AvailableThrust ? "full" : "partial")
                .ToList ());
        Console.WriteLine (string.Join (", ", ratings));
        // [/docs]
    }
}
