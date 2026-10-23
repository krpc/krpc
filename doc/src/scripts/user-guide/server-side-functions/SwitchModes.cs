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
        // A C# lambda cannot hold a loop or a handler, so the expression API builds them
        var engine = Expression.Variable (connection, "engine",
            Type.ClassType (connection, "SpaceCenter", "Engine"));
        var result = Expression.Variable (connection, "result", Type.String (connection));
        var error = Expression.Variable (connection, "error", Type.String (connection));
        var toggleMode = Connection.GetCall (
            (System.Linq.Expressions.Expression<Action>)
            (() => vessel.Parts.Engines [0].ToggleMode ()));
        var switchModes = Expression.BlockWithVariables (connection,
            new List<Expression> { engine, result, error },
            new List<Expression> {
                Expression.TryCatchAll (connection,
                    Expression.Block (connection, new List<Expression> {
                        Expression.ForEach (connection, engine,
                            connection.Compile (() => vessel.Parts.Engines),
                            Expression.CallWithArguments (connection, toggleMode,
                                new Dictionary<int, Expression> { { 0, engine } })),
                        Expression.Assign (connection, result,
                            Expression.ConstantString (connection, "switched"))
                    }),
                    error,
                    Expression.Assign (connection, result,
                        Expression.StringConcat (connection, new List<Expression> {
                            Expression.ConstantString (connection, "could not switch: "),
                            error
                        }))),
                result
            });
        // [/docs]
    }
}
