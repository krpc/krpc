using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Quickload {
    public static void Main () {
        using (var connection = new Connection ()) {
            // [docs]
            var spaceCenter = connection.SpaceCenter ();
            var vessel = spaceCenter.ActiveVessel;
            var parachute =
                vessel.Parts.WithName ("parachuteSingle") [0].Parachute;
            spaceCenter.Quickload ();
            // reads the parachute of the loaded game
            Console.WriteLine ((int)parachute.State);
            // [/docs]
        }
    }
}
