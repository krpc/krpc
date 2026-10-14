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
        var someVessel = spaceCenter.Vessels [0];
        // [docs]
        var target = Connection.GetCall (() => spaceCenter.TargetVessel);
        var expr = Expression.Equal (connection,
            Expression.Call (connection, target),
            Expression.ConstantObject (connection, someVessel.id));
        var evnt = connection.AddEvent (expr);
        // [/docs]
    }
}
