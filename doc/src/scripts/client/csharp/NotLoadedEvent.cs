using System;
using System.Collections.Generic;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
using Type = KRPC.Client.Services.KRPC.Type;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();
        var flight = spaceCenter.ActiveVessel.Flight();

        // A C# lambda cannot hold a handler, so the expression API builds it
        // around a compiled condition
        var above = Expression.Variable(connection, "above", Type.Bool(connection));
        var above1000m = Expression.BlockWithVariables(connection,
            new List<Expression> { above },
            new List<Expression> {
                Expression.TryCatch(connection,
                    Expression.Assign(connection, above,
                        connection.Compile(() => flight.MeanAltitude > 1000)),
                    "KRPC", "ObjectNotLoadedException", null,
                    Expression.Assign(connection, above,
                        Expression.ConstantBool(connection, false))),
                above
            });

        var evnt = connection.AddEvent(above1000m);
        lock (evnt.Condition) {
            evnt.Wait();
            Console.WriteLine("Altitude reached 1000m");
        }
    }
}
