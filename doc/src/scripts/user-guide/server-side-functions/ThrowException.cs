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
        var e = Expression.Bind (connection);
        var amount = Connection.GetCall (() => vessel.Resources.Amount ("LiquidFuel"));
        var expr = e.IfThen (
            e.LessThan (e.Call (amount), e.ConstantDouble (100)),
            e.Throw ("KRPC", "InvalidOperationException", e.ConstantString ("not enough fuel")));
        connection.Run (expr);
        // [/docs]
    }
}
