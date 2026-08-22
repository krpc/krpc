using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();
        var flight = spaceCenter.ActiveVessel.Flight();

        // Create an expression on the server, that computes
        // the vessel's altitude in kilometers
        var expr = Expression.Divide(connection,
            Expression.Call(connection, Connection.GetCall(() => flight.MeanAltitude)),
            Expression.ConstantDouble(connection, 1000));

        // Stream the value of the expression
        var stream = connection.AddStream<double>(expr);
        while (true) {
            Console.WriteLine("Altitude: " + stream.Get() + " km");
            Thread.Sleep(1000);
        }
    }
}
