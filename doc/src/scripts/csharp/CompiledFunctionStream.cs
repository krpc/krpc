using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();
        var flight = spaceCenter.ActiveVessel.Flight();

        // Stream the vessel's altitude in kilometers, computed on the server
        var stream = connection.AddStream(() => flight.MeanAltitude / 1000);
        while (true) {
            Console.WriteLine("Altitude: " + stream.Get() + " km");
            Thread.Sleep(1000);
        }
    }
}
