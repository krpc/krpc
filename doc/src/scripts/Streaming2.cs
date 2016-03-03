using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;
using System;

class Program {
    public static void Main () {
        var connection = new Connection ();
        var spaceCenter = connection.SpaceCenter ();
        var vessel = spaceCenter.ActiveVessel;
        var refframe = vessel.Orbit.Body.ReferenceFrame;
        var position = connection.AddStream(() => vessel.Position(refframe));
        while (true)
            Console.Out.WriteLine(position.Get());
    }
}
