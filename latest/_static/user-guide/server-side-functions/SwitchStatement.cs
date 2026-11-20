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

        // total = 0;
        // foreach (var x in [1, 2, 3])
        //     switch (x) {
        //         case 1: case 2: total += 10; break;
        //         default: total += 1; break;
        //     }
        var total = e.Variable("total", t.Int());
        var x = e.Variable("x", t.Int());
        var values = e.CreateList(new List<Expression> {
            e.ConstantInt(1), e.ConstantInt(2), e.ConstantInt(3)
        });
        var body = e.Match(
            x,
            new List<IList<Expression>> {
                new List<Expression> { e.ConstantInt(1), e.ConstantInt(2) },
                new List<Expression>()
            },
            new List<Expression> {
                e.Assign(total, e.Add(total, e.ConstantInt(10))),
                e.Assign(total, e.Add(total, e.ConstantInt(1)))
            });
        var program = e.BlockWithVariables(
            new List<Expression> { total, x },
            new List<Expression> {
                e.Assign(total, e.ConstantInt(0)),
                e.ForEach(x, values, body),
                total
            });

        Console.WriteLine(connection.Run<int>(program));
    }
}
