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
                .Select (e => e.Thrust).Sum () /
            vessel.Parts.Engines.Where (e => e.Active && e.HasFuel)
                .Select (e => e.Thrust / e.SpecificImpulse).Sum ());
        // [/docs]
    }
}
