using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class SurfacePrograde
{
    public static void Main ()
    {
        var connection = new Connection (name : "Surface prograde");
        var vessel = connection.SpaceCenter ().ActiveVessel;
        var ap = vessel.AutoPilot;

        ap.ReferenceFrame = vessel.SurfaceVelocityReferenceFrame;
        ap.TargetDirection = new Tuple<double,double,double> (0, 1, 0);
        ap.Engage ();
        ap.Wait ();
        ap.Disengage ();
    }
}
