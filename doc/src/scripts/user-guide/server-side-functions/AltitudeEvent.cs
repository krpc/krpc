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
        var evnt = connection.AddEvent (() => flight.MeanAltitude > 1000);
        lock (evnt.Condition) {
            evnt.Wait ();
            Console.WriteLine ("Altitude reached 1000m");
        }
        // [/docs]
    }
}
