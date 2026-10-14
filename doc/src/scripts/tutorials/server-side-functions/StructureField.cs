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
        var siteInfo = Connection.GetCall (() => vessel.Comms.ControlPath [0].Start);
        // [docs]
        // The Name field of a structure a call returns
        var expr = Expression.GetField (connection,
            Expression.Call (connection, siteInfo), "Name");
        // [/docs]
    }
}
