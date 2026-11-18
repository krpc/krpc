using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class CenterOfBuoyancy {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            // [docs]
            var flight = vessel.Flight (vessel.SurfaceReferenceFrame);
            var centerOfBuoyancy = flight.CenterOfBuoyancy;
            var centerOfMass = flight.CenterOfMass;
            // [/docs]
            if (centerOfBuoyancy != null)
                Console.WriteLine (
                    "Trim north = {0:F2} m, east = {1:F2} m",
                    centerOfBuoyancy.Item2 - centerOfMass.Item2,
                    centerOfBuoyancy.Item3 - centerOfMass.Item3);
        }
    }
}
