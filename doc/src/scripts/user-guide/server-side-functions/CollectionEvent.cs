using System;
using System.Collections.Generic;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using Type = KRPC.Client.Services.KRPC.Type;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var e = Expression.Bind(connection);
        var t = Type.Bind(connection);
        var vessel = connection.SpaceCenter().ActiveVessel;

        // A function taking an engine, returning true if it is out of fuel
        var engine = e.Parameter("engine", t.ClassType("SpaceCenter", "Engine"));
        var hasFuel = Connection.GetCall(() => vessel.Parts.Engines[0].HasFuel);
        var outOfFuel = e.Lambda(new List<Expression> { engine },
            e.Not(e.CallWithArguments(hasFuel,
                new Dictionary<int, Expression> { { 0, engine } })));

        // Whether any engine satisfies it
        var engines = Connection.GetCall(() => vessel.Parts.Engines);
        var expr = e.Any(e.Call(engines), outOfFuel);

        var evnt = connection.AddEvent(expr);
        lock (evnt.Condition) {
            evnt.Wait();
            Console.WriteLine("An engine has run out of fuel");
        }
    }
}
