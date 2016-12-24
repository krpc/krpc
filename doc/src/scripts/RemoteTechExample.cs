using System;
using KRPC.Client;
using KRPC.Client.Services.RemoteTech;
using KRPC.Client.Services.SpaceCenter;

class RemoteTechExample
{
    public static void Main ()
    {
        var connection = new Connection ("RemoteTech Example");
        var sc = connection.SpaceCenter ();
        var rt = connection.RemoteTech ();
        var vessel = sc.ActiveVessel;

        // Set a dish target
        var part = vessel.Parts.WithTitle ("Reflectron KR-7") [0];
        var antenna = rt.Antenna (part);
        antenna.TargetBody = sc.Bodies ["Jool"];

        // Get info about the vessels communications
        var comms = rt.Comms (vessel);
        Console.WriteLine ("Signal delay = " + comms.SignalDelay);
    }
}
