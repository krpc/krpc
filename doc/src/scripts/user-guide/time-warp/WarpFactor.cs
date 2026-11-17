using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class WarpFactor {
    public static void Main () {
        using (var connection = new Connection ()) {
            var spaceCenter = connection.SpaceCenter ();
            // [docs]
            Console.WriteLine (spaceCenter.MaximumRailsWarpFactor);
            spaceCenter.RailsWarpFactor = 3;
            // [/docs]
        }
    }
}
