using System;
using System.Linq;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main () {
        var connection = new Connection ();
        var vessel = connection.SpaceCenter ().ActiveVessel;
        // [docs]
        var isp = connection.Run (() =>
            vessel.Parts.Engines.Where (e => e.Active && e.HasFuel)
                .Select (e => e.AvailableThrust).Sum () /
            vessel.Parts.Engines.Where (e => e.Active && e.HasFuel)
                .Select (e => e.AvailableThrust / e.SpecificImpulse)
                .Sum ());
        // [/docs]
        Console.WriteLine ("Combined Isp = {0:F0} seconds", isp);
    }
}
