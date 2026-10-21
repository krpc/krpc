using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();
        var vessel = spaceCenter.ActiveVessel;
        var refFrame = vessel.Orbit.Body.ReferenceFrame;
        while (true)
            Console.WriteLine(vessel.Position(refFrame));
    }
}
