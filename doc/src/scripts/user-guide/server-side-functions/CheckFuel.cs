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
        // A C# lambda cannot throw, so the expression API builds the statement
        // around a compiled condition
        var checkFuel = Expression.IfThen (connection,
            connection.Compile (() => vessel.Resources.Amount ("LiquidFuel") < 100),
            Expression.Throw (connection, "KRPC", "InvalidOperationException",
                Expression.ConstantString (connection, "not enough fuel")));
        // [/docs]
    }
}
