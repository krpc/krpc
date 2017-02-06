using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;
using Vector3 = System.Tuple<double, double, double>;

class AngleOfAttack
{
    static Vector3 CrossProduct (Vector3 u, Vector3 v)
    {
        return new Vector3 (
            u.Item2 * v.Item3 - u.Item3 * v.Item2,
            u.Item3 * v.Item1 - u.Item1 * v.Item3,
            u.Item1 * v.Item2 - u.Item2 * v.Item1
        );
    }

    static double DotProduct (Vector3 u, Vector3 v)
    {
        return u.Item1 * v.Item1 + u.Item2 * v.Item2 + u.Item3 * v.Item3;
    }

    static double Magnitude (Vector3 v)
    {
        return Math.Sqrt (DotProduct (v, v));
    }

    // Compute the angle between vector x and y
    static double AngleBetweenVectors (Vector3 u, Vector3 v)
    {
        double dp = DotProduct (u, v);
        if (dp == 0)
            return 0;
        double um = Magnitude (u);
        double vm = Magnitude (v);
        return Math.Acos (dp / (um * vm)) * (180f / Math.PI);
    }

    public static void Main ()
    {
        var conn = new Connection ("Angle of attack");
        var vessel = conn.SpaceCenter ().ActiveVessel;

        while (true) {
            var vesselDirection = vessel.Direction (vessel.SurfaceReferenceFrame);

            // Get the direction of the vessel in the horizon plane
            var horizonDirection = new Vector3 (
                0, vesselDirection.Item2, vesselDirection.Item3);

            // Compute the pitch - the angle between the vessels direction and
            // the direction in the horizon plane
            double pitch = AngleBetweenVectors (vesselDirection, horizonDirection);
            if (vesselDirection.Item1 < 0)
                pitch = -pitch;

            // Compute the heading - the angle between north and
            // the direction in the horizon plane
            var north = new Vector3 (0, 1, 0);
            double heading = AngleBetweenVectors (north, horizonDirection);
            if (horizonDirection.Item3 < 0)
                heading = 360 - heading;

            // Compute the roll
            // Compute the plane running through the vessels direction
            // and the upwards direction
            var up = new Vector3 (1, 0, 0);
            var planeNormal = CrossProduct (vesselDirection, up);
            // Compute the upwards direction of the vessel
            var vesselUp = conn.SpaceCenter ().TransformDirection (
                new Vector3 (0, 0, -1),
                vessel.ReferenceFrame, vessel.SurfaceReferenceFrame);
            // Compute the angle between the upwards direction of
            // the vessel and the plane normal
            double roll = AngleBetweenVectors (vesselUp, planeNormal);
            // Adjust so that the angle is between -180 and 180 and
            // rolling right is +ve and left is -ve
            if (vesselUp.Item1 > 0)
                roll *= -1;
            else if (roll < 0)
                roll += 180;
            else
                roll -= 180;

            Console.WriteLine ("pitch = {0:F1}, heading = {1:F1}, roll = {2:F1}",
                               pitch, heading, roll);

            System.Threading.Thread.Sleep (1000);
        }
    }
}
