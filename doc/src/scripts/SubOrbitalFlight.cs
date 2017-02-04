using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class SubOrbitalFlight
{
    public static void Main ()
    {
        var conn = new Connection ("Sub-orbital flight");

        var vessel = conn.SpaceCenter ().ActiveVessel;

        vessel.AutoPilot.TargetPitchAndHeading (90, 90);
        vessel.AutoPilot.Engage ();
        vessel.Control.Throttle = 1;
        System.Threading.Thread.Sleep (1000);

        Console.WriteLine ("Launch!");
        vessel.Control.ActivateNextStage ();

        while (vessel.Resources.Amount("SolidFuel") > 0.1)
            System.Threading.Thread.Sleep (1000);
        Console.WriteLine ("Booster separation");
        vessel.Control.ActivateNextStage ();

        while (vessel.Flight ().MeanAltitude < 10000)
            System.Threading.Thread.Sleep (1000);

        Console.WriteLine ("Gravity turn");
        vessel.AutoPilot.TargetPitchAndHeading (60, 90);

        while (vessel.Orbit.ApoapsisAltitude < 100000)
            System.Threading.Thread.Sleep (1000);
        Console.WriteLine ("Launch stage separation");
        vessel.Control.Throttle = 0;
        System.Threading.Thread.Sleep (1000);
        vessel.Control.ActivateNextStage ();
        vessel.AutoPilot.Disengage ();

        while (vessel.Flight ().SurfaceAltitude > 1000)
            System.Threading.Thread.Sleep (1000);
        vessel.Control.ActivateNextStage ();

        while (vessel.Flight (vessel.Orbit.Body.ReferenceFrame).VerticalSpeed < -0.1) {
            Console.WriteLine ("Altitude = {0:F1} meters", vessel.Flight ().SurfaceAltitude);
            System.Threading.Thread.Sleep (1000);
        }
        Console.WriteLine ("Landed!");
        conn.Dispose();
    }
}
