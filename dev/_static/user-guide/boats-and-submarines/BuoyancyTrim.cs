using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class BuoyancyTrim {
    public static void Main () {
        using (var connection = new Connection ("Buoyancy Trim")) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var body = vessel.Orbit.Body;

            if (!body.HasOcean)
                throw new InvalidOperationException (
                    body.Name + " has no ocean");

            var flight = vessel.Flight (vessel.SurfaceReferenceFrame);

            // The fully submerged figures, which do not depend on where
            // the vessel is
            var buoyancy = flight.BuoyantForceAt (
                body.OceanDensity, body.SurfaceGravity);
            var torque = flight.BuoyantTorqueAt (
                body.OceanDensity, body.SurfaceGravity);
            var weight = vessel.Mass * body.SurfaceGravity;
            var torqueSize = Math.Sqrt (torque.Item1 * torque.Item1 +
                                        torque.Item2 * torque.Item2 +
                                        torque.Item3 * torque.Item3);

            Console.WriteLine ("Displacement:  {0:F1} m^3",
                               flight.Displacement);
            Console.WriteLine ("Buoyancy:      {0:F0} N fully submerged",
                               buoyancy);
            Console.WriteLine ("Weight:        {0:F0} N", weight);
            Console.WriteLine ("Floats:        {0}",
                               buoyancy > weight ? "true" : "false");
            Console.WriteLine ("Torque:        {0:F0} N m fully submerged",
                               torqueSize);

            // The trim, read in the surface reference frame. Its x axis
            // points up, so the y and z axes span the horizontal. A
            // horizontal offset between the two centers tips the hull
            // until they line up again.
            var centerOfBuoyancy = flight.CenterOfBuoyancy;

            if (centerOfBuoyancy == null) {
                Console.WriteLine (
                    "Trim:          vessel is out of the water");
            } else {
                var centerOfMass = flight.CenterOfMass;
                var north = centerOfBuoyancy.Item2 - centerOfMass.Item2;
                var east = centerOfBuoyancy.Item3 - centerOfMass.Item3;
                Console.WriteLine ("Submerged:     {0:F0}%",
                                   100 * flight.SubmergedPortion);
                Console.WriteLine ("Trim:          {0:F2} m horizontally",
                                   Math.Sqrt (north * north + east * east));
                Console.WriteLine ("Trim north:    {0:+0.00;-0.00} m",
                                   north);
                Console.WriteLine ("Trim east:     {0:+0.00;-0.00} m",
                                   east);
            }
        }
    }
}
