using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();
        var flight = spaceCenter.ActiveVessel.Flight();

        // Create an event that triggers when the lambda returns true
        var evnt = connection.AddEvent(() => flight.MeanAltitude > 1000);

        // Wait on the event
        lock (evnt.Condition) {
            evnt.Wait();
            Console.WriteLine("Altitude reached 1000m");
        }
    }
}
