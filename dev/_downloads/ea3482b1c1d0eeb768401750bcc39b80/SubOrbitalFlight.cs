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
        vessel.AutoPilot.Engaged = true;
        vessel.Control.Throttle = 1;
        System.Threading.Thread.Sleep (1000);

        Console.WriteLine ("Launch!");
        vessel.Control.ActivateNextStage ();

        {
            var evnt = conn.AddEvent (() => vessel.Resources.Amount ("SolidFuel") < 0.1f);
            lock (evnt.Condition) {
                evnt.Wait ();
            }
        }
        Console.WriteLine ("Booster separation");
        vessel.Control.ActivateNextStage ();

        var flight = vessel.Flight ();
        {
            var evnt = conn.AddEvent (() => flight.MeanAltitude > 10000);
            lock (evnt.Condition) {
                evnt.Wait ();
            }
        }

        Console.WriteLine ("Gravity turn");
        vessel.AutoPilot.TargetPitchAndHeading (60, 90);

        {
            var evnt = conn.AddEvent (() => vessel.Orbit.ApoapsisAltitude > 100000);
            lock (evnt.Condition) {
                evnt.Wait ();
            }
        }

        Console.WriteLine ("Launch stage separation");
        vessel.Control.Throttle = 0;
        System.Threading.Thread.Sleep (1000);
        vessel.Control.ActivateNextStage ();
        vessel.AutoPilot.Engaged = false;

        {
            var evnt = conn.AddEvent (() => flight.SurfaceAltitude < 1000);
            lock (evnt.Condition) {
                evnt.Wait ();
            }
        }

        vessel.Control.ActivateNextStage ();

        while (vessel.Flight (vessel.Orbit.Body.ReferenceFrame).VerticalSpeed < -0.1) {
            Console.WriteLine ("Altitude = {0:F1} meters", flight.SurfaceAltitude);
            System.Threading.Thread.Sleep (1000);
        }
        Console.WriteLine ("Landed!");
        conn.Dispose ();
    }
}
