using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var vessel = connection.SpaceCenter().ActiveVessel;
        var flightInfo = vessel.Flight(vessel.Orbit.Body.ReferenceFrame);

        // Both threads make calls over the same connection
        var threads = new[] {
            new Thread(() => {
                for (var i = 0; i < 10; i++)
                    Console.WriteLine("Altitude: " + flightInfo.MeanAltitude);
            }),
            new Thread(() => {
                for (var i = 0; i < 10; i++)
                    Console.WriteLine("Speed: " + flightInfo.Speed);
            })
        };
        foreach (var thread in threads)
            thread.Start();
        foreach (var thread in threads)
            thread.Join();
    }
}
