using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class BuoyantForce {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            // [docs]
            var body = vessel.Orbit.Body;
            var flight = vessel.Flight ();
            var displacement = flight.Displacement;
            var buoyancy = flight.BuoyantForceAt (
                body.OceanDensity, body.SurfaceGravity);
            // [/docs]
            Console.WriteLine (
                "Displacement = {0:F1} m^3, buoyancy = {1:F0} N",
                displacement, buoyancy);
        }
    }
}
