using System.Collections.Generic;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var flight = connection.SpaceCenter().ActiveVessel.Flight();

        // [docs]
        var e = Expression.Bind(connection);
        var altitude = e.Variable("altitude", Type.Bind(connection).Double());
        var function = e.BlockWithVariables(
            new List<Expression> { altitude },
            new List<Expression> {
                e.TryCatch(
                    e.Assign(altitude, connection.Compile(() => flight.MeanAltitude / 1000)),
                    "KRPC", "ObjectNotLoadedException", null,
                    e.Assign(altitude, e.ConstantDouble(0))),
                altitude
            });
        var stream = connection.AddStream<double>(function);
        // [/docs]
        System.Console.WriteLine(stream.Get());
    }
}
