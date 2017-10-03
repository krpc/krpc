using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var krpc = connection.KRPC();
        var spaceCenter = connection.SpaceCenter();
        var flight = spaceCenter.ActiveVessel.Flight();

        // Get the remote procedure call as a message object,
        // so it can be passed to the server
        var meanAltitude = Connection.GetCall(() => flight.MeanAltitude);

        // Create an expression on the server
        var expr = Expression.GreaterThan(connection,
            Expression.Call(connection, meanAltitude),
            Expression.ConstantDouble(connection, 1000));

        var evnt = krpc.AddEvent(expr);
        lock (evnt.Condition) {
            evnt.Wait();
            Console.WriteLine("Altitude reached 1000m");
        }
    }
}
