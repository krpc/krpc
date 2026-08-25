using System;
using System.Collections.Generic;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using Type = KRPC.Client.Services.KRPC.Type;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main () {
        var connection = new Connection ();
        var krpc = connection.KRPC ();
        var vessel = connection.SpaceCenter ().ActiveVessel;

        // A function taking an engine, returning true if it is out of fuel
        var engine = Expression.Parameter (connection, "engine",
            Type.ClassType (connection, "SpaceCenter", "Engine"));
        var hasFuel = Connection.GetCall (() => vessel.Parts.Engines [0].HasFuel);
        var outOfFuel = Expression.Lambda (connection,
            new List<Expression> { engine },
            Expression.Not (connection,
                Expression.CallWithArguments (connection, hasFuel,
                    new Dictionary<int, Expression> { { 0, engine } })));

        // Whether any engine satisfies it
        var engines = Connection.GetCall (() => vessel.Parts.Engines);
        var expr = Expression.Any (connection,
            Expression.Call (connection, engines), outOfFuel);

        var evnt = krpc.AddEvent (expr);
        lock (evnt.Condition) {
            evnt.Wait ();
            Console.WriteLine ("An engine has run out of fuel");
        }
    }
}
