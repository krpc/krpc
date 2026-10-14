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
        // The vessel's altitude, in kilometers, computed on the server
        var stream = connection.AddStream (() => flight.MeanAltitude / 1000);
        Console.WriteLine ("Altitude: " + stream.Get () + " km");
        // [/docs]
    }
}
