using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var flight = connection.SpaceCenter().ActiveVessel.Flight();

        var evnt = connection.AddEvent(() => flight.MeanAltitude > 1000);
        evnt.AddCallback(
            () => {
                Console.WriteLine("Altitude reached 1000m");
            });

        // Keep the program running, while the callback is called
        while (true)
            Thread.Sleep(1000);
    }
}
