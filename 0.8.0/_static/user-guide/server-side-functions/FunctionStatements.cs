using System;
using System.Collections.Generic;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using Type = KRPC.Client.Services.KRPC.Type;

class Program {
    public static void Main() {
        var connection = new Connection();
        var e = Expression.Bind(connection);
        var t = Type.Bind(connection);

        // total = 0; foreach (var x in [1, 2, 3]) total += x;
        var total = e.Variable("total", t.Int());
        var x = e.Variable("x", t.Int());
        var values = e.CreateList(new List<Expression> {
            e.ConstantInt(1), e.ConstantInt(2), e.ConstantInt(3)
        });
        var program = e.BlockWithVariables(
            new List<Expression> { total, x },
            new List<Expression> {
                e.Assign(total, e.ConstantInt(0)),
                e.ForEach(x, values, e.Assign(total, e.Add(total, x))),
                total
            });

        Console.WriteLine(connection.Run<int>(program));
    }
}
