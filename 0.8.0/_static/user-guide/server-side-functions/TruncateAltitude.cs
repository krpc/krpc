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
        var meanAltitude = Connection.GetCall (() => flight.MeanAltitude);
        // [docs]
        var e = Expression.Bind (connection);
        var t = Type.Bind (connection);
        // Truncate the altitude to a whole number of meters
        var expr = e.Cast (e.Call (meanAltitude), t.Int ());
        // [/docs]
    }
}
