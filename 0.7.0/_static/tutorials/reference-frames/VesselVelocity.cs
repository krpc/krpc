using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class VesselVelocity
{
    public static void Main ()
    {
        var connection = new Connection (name : "Vessel velocity");
        var vessel = connection.SpaceCenter ().ActiveVessel;
        var refFrame = ReferenceFrame.CreateHybrid(
          connection,
          vessel.Orbit.Body.ReferenceFrame,
          vessel.SurfaceReferenceFrame);

        while (true) {
            var velocity = vessel.Flight (refFrame).Velocity;
            Console.WriteLine ("Surface velocity = ({0:F1}, {1:F1}, {2:F1})",
                               velocity.Item1, velocity.Item2, velocity.Item3);
            System.Threading.Thread.Sleep (1000);
        }
    }
}
