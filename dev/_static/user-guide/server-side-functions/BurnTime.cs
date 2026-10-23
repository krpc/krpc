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
        var burnTime = connection.Compile ((double deltaV) =>
            (vessel.Mass - vessel.Mass / Math.Exp (deltaV / (vessel.SpecificImpulse * 9.82)))
                * vessel.SpecificImpulse * 9.82 / vessel.AvailableThrust);
        foreach (var deltaV in new [] { 100.0, 250.0, 400.0 })
            Console.WriteLine (connection.Run<double> (burnTime, deltaV));
        // [/docs]
    }
}
