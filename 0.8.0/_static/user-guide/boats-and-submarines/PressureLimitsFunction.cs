using System;
using System.Linq;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class PressureLimitsFunction {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            // [docs]
            var overLimit = connection.Run (
                () => vessel.Parts.All
                    .Where (part =>
                        part.StaticPressure + part.DynamicPressure >
                        part.MaxPressure)
                    .Select (part => part.Title)
                    .ToList ());
            // [/docs]
            foreach (var title in overLimit)
                Console.WriteLine (title);
        }
    }
}
