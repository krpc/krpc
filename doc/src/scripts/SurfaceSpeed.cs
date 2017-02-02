using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class SurfaceSpeed
{
    public static void Main ()
    {
        var connection = new Connection (name : "Surface speed");
        var vessel = connection.SpaceCenter ().ActiveVessel;
        var refFrame = vessel.Orbit.Body.ReferenceFrame;

        while (true) {
            var velocity = vessel.Flight (refFrame).Velocity;
            Console.WriteLine ("Surface velocity = ({0:F1}, {1:F1}, {2:F1})",
                               velocity.Item1, velocity.Item2, velocity.Item3);

            var speed = vessel.Flight (refFrame).Speed;
            Console.WriteLine ("Surface speed = {0:F1} m/s", speed);

            System.Threading.Thread.Sleep (1000);
        }
    }
}
