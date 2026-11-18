using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class DestroyedPart {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var part = vessel.Parts.All [0];
            // [docs]
            try {
                Console.WriteLine (part.Temperature);
            } catch (ObjectDestroyedException) {
                Console.WriteLine ("that part is gone");
            }
            // [/docs]
        }
    }
}
