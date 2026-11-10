using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var e = Expression.Bind(connection);
        var spaceCenter = connection.SpaceCenter();
        var flight = spaceCenter.ActiveVessel.Flight();

        // Build a function that computes
        // the vessel's altitude in kilometers
        var expr = e.Divide(
            e.Call(Connection.GetCall(() => flight.MeanAltitude)), e.ConstantDouble(1000));

        // Stream the value of the function
        var stream = connection.AddStream<double>(expr);
        while (true) {
            Console.WriteLine("Altitude: " + stream.Get() + " km");
            Thread.Sleep(1000);
        }
    }
}
