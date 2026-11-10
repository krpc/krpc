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
        var e = Expression.Bind (connection);
        var target = Connection.GetCall (() => spaceCenter.TargetVessel);
        var expr = e.Equal (
            e.Call (target), e.ConstantObject (RemoteObject.GetObjectId (someVessel)));
        var evnt = connection.AddEvent (expr);
        // [/docs]
    }
}
