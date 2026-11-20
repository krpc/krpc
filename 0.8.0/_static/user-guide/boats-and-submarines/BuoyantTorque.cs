using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class BuoyantTorque {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var body = vessel.Orbit.Body;
            var flight = vessel.Flight (vessel.SurfaceReferenceFrame);
            // [docs]
            var torque = flight.BuoyantTorqueAt (
                body.OceanDensity, body.SurfaceGravity);
            // [/docs]
            Console.WriteLine (
                "Torque = ({0:F0}, {1:F0}, {2:F0}) N m",
                torque.Item1, torque.Item2, torque.Item3);
        }
    }
}
