using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();
        var flight = spaceCenter.ActiveVessel.Flight();
        using (var altitude = connection.AddStream(() => flight.MeanAltitude)) {
            for (var i = 0; i < 10; i++)
                Console.WriteLine(altitude.Get());
        }
        // The stream was removed at the end of the block
    }
}
