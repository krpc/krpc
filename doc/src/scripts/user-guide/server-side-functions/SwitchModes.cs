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
        var t = Type.Bind (connection);
        // A C# lambda cannot hold a loop or a handler, so the expression API builds them
        var engine = e.Variable ("engine", t.ClassType ("SpaceCenter", "Engine"));
        var result = e.Variable ("result", t.String ());
        var error = e.Variable ("error", t.String ());
        var toggleMode = Connection.GetCall (
            (System.Linq.Expressions.Expression<Action>)
            (() => vessel.Parts.Engines [0].ToggleMode ()));
        var switchModes = e.BlockWithVariables (
            new List<Expression> { engine, result, error },
            new List<Expression> {
                e.TryCatchAll (
                    e.Block (new List<Expression> {
                        e.ForEach (engine,
                            connection.Compile (() => vessel.Parts.Engines),
                            e.CallWithArguments (toggleMode,
                                new Dictionary<int, Expression> { { 0, engine } })),
                        e.Assign (result, e.ConstantString ("switched"))
                    }),
                    error,
                    e.Assign (result,
                        e.StringConcat (new List<Expression> {
                            e.ConstantString ("could not switch: "),
                            error
                        }))),
                result
            });
        // [/docs]
    }
}
