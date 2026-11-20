using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class AngleOfAttack
{
    public static void Main ()
    {
        var conn = new Connection ("Angle of attack");
        var vessel = conn.SpaceCenter ().ActiveVessel;

        while (true) {
            var d = vessel.Direction (vessel.Orbit.Body.ReferenceFrame);
            var v = vessel.Velocity (vessel.Orbit.Body.ReferenceFrame);

            // Compute the dot product of d and v
            var dotProd =
                d.Item1 * v.Item1 + d.Item2 * v.Item2 + d.Item3 * v.Item3;

            // Compute the magnitude of v, as d is a unit vector
            var vMag = Math.Sqrt (
                v.Item1 * v.Item1 + v.Item2 * v.Item2 + v.Item3 * v.Item3);

            // Compute the angle between the vectors
            double angle = 0;
            if (vMag > 0)
                angle = Math.Acos (dotProd / vMag) * (180.0 / Math.PI);

            Console.WriteLine ("Angle of attack = {0:F1} degrees", angle);

            System.Threading.Thread.Sleep (1000);
        }
    }
}
