using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();
        var flight = spaceCenter.ActiveVessel.Flight();
        var expr = Expression.GreaterThan(connection,
            Expression.Call(connection, Connection.GetCall(() => flight.MeanAltitude)),
            Expression.ConstantDouble(connection, 1000));
        using (var evnt = connection.AddEvent(expr)) {
            lock (evnt.Condition) {
                evnt.Wait();
                Console.WriteLine("Altitude reached 1000m");
            }
        }
        // The event was removed at the end of the block
    }
}
