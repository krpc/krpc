using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class VesselPosition
{
    public static void Main ()
    {
        var connection = new Connection ();
        var vessel = connection.SpaceCenter ().ActiveVessel;
        Console.WriteLine (vessel.Position (vessel.Orbit.Body.ReferenceFrame));
    }
}
