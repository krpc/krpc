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
        var flight = vessel.Flight ();
        // [docs]
        // A single side-effecting call, run on the server
        connection.Run (() => vessel.Control.ToggleActionGroup (1));

        // Compute and return a value in one tick
        var altitude = connection.Run (() => flight.MeanAltitude / 1000);
        // [/docs]
    }
}
