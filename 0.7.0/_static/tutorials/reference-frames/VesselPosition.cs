using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class VesselPosition
{
    public static void Main ()
    {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var position = vessel.Position (vessel.Orbit.Body.ReferenceFrame);
            Console.WriteLine ("({0:F1}, {1:F1}, {2:F1})",
                               position.Item1, position.Item2, position.Item3);
        }
    }
}
