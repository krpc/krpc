using System;
using System.Linq;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class ControlFromHere
{
    public static void Main ()
    {
        var conn = new Connection ();
        var vessel = conn.SpaceCenter ().ActiveVessel;
        var part = vessel.Parts.WithTitle ("Clamp-O-Tron Docking Port") [0];
        vessel.Parts.Controlling = part;
    }
}
