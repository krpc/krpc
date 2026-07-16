using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        using (var connection = new Connection()) {
            var spaceCenter = connection.SpaceCenter();
            var vessel = spaceCenter.ActiveVessel;
            vessel.Name = "My Vessel";
            var flightInfo = vessel.Flight();
            Console.WriteLine(flightInfo.MeanAltitude);
        }
    }
}
