using System;
using System.Collections.Generic;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using Type = KRPC.Client.Services.KRPC.Type;

class Program {
    public static void Main () {
        var connection = new Connection ();

        // total = 0; foreach (var x in [1, 2, 3]) total += x;
        var total = Expression.Variable (connection, "total", Type.Int (connection));
        var x = Expression.Variable (connection, "x", Type.Int (connection));
        var values = Expression.CreateList (connection, new List<Expression> {
            Expression.ConstantInt (connection, 1),
            Expression.ConstantInt (connection, 2),
            Expression.ConstantInt (connection, 3)
        });
        var program = Expression.BlockWithVariables (
            connection,
            new List<Expression> { total, x },
            new List<Expression> {
                Expression.Assign (connection, total, Expression.ConstantInt (connection, 0)),
                Expression.ForEach (connection, x, values,
                    Expression.Assign (connection, total,
                        Expression.Add (connection, total, x))),
                total
            });

        Console.WriteLine (connection.RunFunction<int> (program));
    }
}
