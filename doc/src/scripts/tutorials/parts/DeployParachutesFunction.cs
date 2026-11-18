using System.Collections.Generic;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
using Type = KRPC.Client.Services.KRPC.Type;

class Program {
    public static void Main () {
        var connection = new Connection ();
        var vessel = connection.SpaceCenter ().ActiveVessel;
        // [docs]
        var e = Expression.Bind (connection);
        var t = Type.Bind (connection);

        // foreach (var parachute in vessel.Parts.Parachutes)
        //     parachute.Deploy ();
        var parachute = e.Variable (
            "parachute", t.ClassType ("SpaceCenter", "Parachute"));
        var deploy = e.CallWithArguments (
            Connection.GetCallTemplate ((Parachute p) => p.Deploy ()),
            new Dictionary<int, Expression> { { 0, parachute } });
        var parachutes = Connection.GetCall (() => vessel.Parts.Parachutes);
        var program = e.BlockWithVariables (
            new List<Expression> { parachute },
            new List<Expression> {
                e.ForEach (parachute, e.Call (parachutes), deploy)
            });

        connection.Run (program);
        // [/docs]
    }
}
