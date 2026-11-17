using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;
using KRPC.Client.Services.StdLib;

class AngleToPrograde {
    public static void Main () {
        using (var connection = new Connection ()) {
            var stdLib = connection.StdLib ();
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var frame = vessel.Orbit.Body.NonRotatingReferenceFrame;

            // [docs]
            var direction = vessel.Direction (frame);
            var prograde = vessel.Flight (frame).Prograde;
            var angle = stdLib.VectorAngle (direction, prograde);
            Console.WriteLine (stdLib.RadiansToDegrees (angle));
            // [/docs]
        }
    }
}
