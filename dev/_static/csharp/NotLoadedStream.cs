using System.Collections.Generic;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var flight = connection.SpaceCenter().ActiveVessel.Flight();

        // [docs]
        var altitude = Expression.Variable(connection, "altitude", Type.Double(connection));
        var function = Expression.BlockWithVariables(connection,
            new List<Expression> { altitude },
            new List<Expression> {
                Expression.TryCatch(connection,
                    Expression.Assign(connection, altitude,
                        connection.Compile(() => flight.MeanAltitude / 1000)),
                    "KRPC", "ObjectNotLoadedException", null,
                    Expression.Assign(connection, altitude,
                        Expression.ConstantDouble(connection, 0))),
                altitude
            });
        var stream = connection.AddStream<double>(function);
        // [/docs]
        System.Console.WriteLine(stream.Get());
    }
}
