using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class LaunchVessel {
    public static void Main () {
        using (var connection = new Connection ()) {
            var spaceCenter = connection.SpaceCenter ();
            // [docs]
            foreach (var name in spaceCenter.LaunchableVessels ("VAB"))
                Console.WriteLine (name);
            spaceCenter.LaunchVesselFromVAB ("Kerbal X");
            Console.WriteLine (spaceCenter.ActiveVessel.Name);
            // [/docs]
        }
    }
}
