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
        var amount = Connection.GetCall (() => vessel.Resources.Amount ("LiquidFuel"));
        var expr = Expression.IfThen (connection,
            Expression.LessThan (connection,
                Expression.Call (connection, amount),
                Expression.ConstantDouble (connection, 100)),
            Expression.Throw (connection, "KRPC", "InvalidOperationException",
                Expression.ConstantString (connection, "not enough fuel")));
        connection.Run (expr);
        // [/docs]
    }
}
