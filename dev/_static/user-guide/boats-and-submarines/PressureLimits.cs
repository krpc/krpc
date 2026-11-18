using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class PressureLimits {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            // [docs]
            foreach (var part in vessel.Parts.All)
                if (part.StaticPressure + part.DynamicPressure >
                    part.MaxPressure)
                    Console.WriteLine (
                        "{0} is over its pressure limit", part.Title);
            // [/docs]
        }
    }
}
