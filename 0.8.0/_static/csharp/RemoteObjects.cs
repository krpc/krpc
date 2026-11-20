using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();
        var vessel = spaceCenter.ActiveVessel;
        var flightInfo = vessel.Flight();

        // Each read gets the current altitude from the game
        Console.WriteLine(flightInfo.MeanAltitude);
        Thread.Sleep(1000);
        Console.WriteLine(flightInfo.MeanAltitude);

        // Two objects for the same vessel are equal
        Console.WriteLine(vessel == spaceCenter.ActiveVessel);
    }
}
