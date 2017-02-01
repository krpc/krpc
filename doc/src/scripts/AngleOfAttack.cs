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
            var dotProd = d.Item1 * v.Item1 + d.Item2 * v.Item2 + d.Item3 * v.Item3;

            // Compute the magnitude of v
            var vMag = Math.Sqrt (
                v.Item1 * v.Item1 + v.Item2 * v.Item2 + v.Item3 * v.Item3);
            // Note: don't need to magnitude of d as it is a unit vector

            // Compute the angle between the vectors
            double angle = 0;
            if (dotProd > 0)
                angle = Math.Abs (Math.Acos (dotProd / vMag) * (180.0 / Math.PI));

            Console.WriteLine (
                "Angle of attack = " + Math.Round (angle, 2) + " degrees");

            System.Threading.Thread.Sleep (1000);
        }
    }
}
