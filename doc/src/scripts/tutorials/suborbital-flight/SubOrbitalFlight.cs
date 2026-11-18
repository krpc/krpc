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
        vessel.Control.Throttle = 0.5f;
        System.Threading.Thread.Sleep (1000);

        Console.WriteLine ("Launch!");
        vessel.Control.ActivateNextStage ();

        var flight = vessel.Flight ();
        {
            var evnt = conn.AddEvent (() => flight.MeanAltitude > 500);
            lock (evnt.Condition) {
                evnt.Wait ();
            }
        }

        Console.WriteLine ("Pitching over");
        vessel.AutoPilot.TargetSmoothingTime = 10;
        vessel.AutoPilot.TargetPitchAndHeading (70, 90);

        {
            var evnt = conn.AddEvent (
                () => vessel.Resources.Amount ("LiquidFuel") < 0.1f);
            lock (evnt.Condition) {
                evnt.Wait ();
            }
        }

        Console.WriteLine ("Flame out");
        vessel.Control.Throttle = 0;
        vessel.AutoPilot.ReferenceFrame =
            vessel.SurfaceVelocityReferenceFrame;
        vessel.AutoPilot.TargetDirection = Tuple.Create (0.0, 1.0, 0.0);

        var bodyFlight = vessel.Flight (vessel.Orbit.Body.ReferenceFrame);
        {
            var evnt = conn.AddEvent (() => bodyFlight.VerticalSpeed < 0);
            lock (evnt.Condition) {
                evnt.Wait ();
            }
        }

        Console.WriteLine ("Apoapsis reached, separating");
        vessel.Control.ActivateNextStage ();
        vessel.AutoPilot.TargetDirection = Tuple.Create (0.0, -1.0, 0.0);
        vessel.AutoPilot.Wait ();
        vessel.AutoPilot.Engaged = false;

        {
            var evnt = conn.AddEvent (() => flight.SurfaceAltitude < 2000);
            lock (evnt.Condition) {
                evnt.Wait ();
            }
        }

        Console.WriteLine ("Deploying the parachute");
        vessel.Parts.Parachutes [0].DeployAltitude = 550;
        vessel.Control.ActivateNextStage ();

        while (vessel.Situation != VesselSituation.Landed &&
               vessel.Situation != VesselSituation.Splashed) {
            Console.WriteLine ("Altitude = {0:F1} meters",
                               flight.SurfaceAltitude);
            System.Threading.Thread.Sleep (1000);
        }
        Console.WriteLine ("Landed!");
        conn.Dispose ();
    }
}
