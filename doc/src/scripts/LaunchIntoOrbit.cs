using System;
using System.Collections.Generic;
using System.Net;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class LaunchIntoOrbit
{
    public static void Main ()
    {
        var conn = new Connection ("Launch into orbit");
        var vessel = conn.SpaceCenter ().ActiveVessel;

        float turnStartAltitude = 250;
        float turnEndAltitude = 45000;
        float targetAltitude = 150000;

        // Set up streams for telemetry
        var ut = conn.AddStream (() => conn.SpaceCenter ().UT);
        var flight = vessel.Flight ();
        var altitude = conn.AddStream (() => flight.MeanAltitude);
        var apoapsis = conn.AddStream (() => vessel.Orbit.ApoapsisAltitude);
        var stage3Resources =
            vessel.ResourcesInDecoupleStage (stage: 3, cumulative: false);
        var srbFuel = conn.AddStream(() => stage3Resources.Amount("SolidFuel"));

        // Pre-launch setup
        vessel.Control.SAS = false;
        vessel.Control.RCS = false;
        vessel.Control.Throttle = 1;

        // Countdown...
        Console.WriteLine ("3...");
        System.Threading.Thread.Sleep (1000);
        Console.WriteLine ("2...");
        System.Threading.Thread.Sleep (1000);
        Console.WriteLine ("1...");
        System.Threading.Thread.Sleep (1000);
        Console.WriteLine ("Launch!");

        // Activate the first stage
        vessel.Control.ActivateNextStage ();
        vessel.AutoPilot.Engage ();
        vessel.AutoPilot.TargetPitchAndHeading (90, 90);

        // Main ascent loop
        bool srbsSeparated = false;
        double turnAngle = 0;
        while (true) {

            // Gravity turn
            if (altitude.Get () > turnStartAltitude &&
                altitude.Get () < turnEndAltitude) {
                double frac = (altitude.Get () - turnStartAltitude)
                              / (turnEndAltitude - turnStartAltitude);
                double newTurnAngle = frac * 90.0;
                if (Math.Abs (newTurnAngle - turnAngle) > 0.5) {
                    turnAngle = newTurnAngle;
                    vessel.AutoPilot.TargetPitchAndHeading (
                        (float)(90 - turnAngle), 90);
                }
            }

            // Separate SRBs when finished
            if (!srbsSeparated) {
                if (srbFuel.Get () < 0.1) {
                    vessel.Control.ActivateNextStage ();
                    srbsSeparated = true;
                    Console.WriteLine ("SRBs separated");
                }
            }

            // Decrease throttle when approaching target apoapsis
            if (apoapsis.Get () > targetAltitude * 0.9) {
                Console.WriteLine ("Approaching target apoapsis");
                break;
            }
        }

        // Disable engines when target apoapsis is reached
        vessel.Control.Throttle = 0.25f;
        while (apoapsis.Get () < targetAltitude) {
        }
        Console.WriteLine ("Target apoapsis reached");
        vessel.Control.Throttle = 0;

        // Wait until out of atmosphere
        Console.WriteLine ("Coasting out of atmosphere");
        while (altitude.Get () < 70500) {
        }

        // Plan circularization burn (using vis-viva equation)
        Console.WriteLine ("Planning circularization burn");
        double mu = vessel.Orbit.Body.GravitationalParameter;
        double r = vessel.Orbit.Apoapsis;
        double a1 = vessel.Orbit.SemiMajorAxis;
        double a2 = r;
        double v1 = Math.Sqrt (mu * ((2.0 / r) - (1.0 / a1)));
        double v2 = Math.Sqrt (mu * ((2.0 / r) - (1.0 / a2)));
        double deltaV = v2 - v1;
        var node = vessel.Control.AddNode (
            ut.Get () + vessel.Orbit.TimeToApoapsis, prograde: (float)deltaV);

        // Calculate burn time (using rocket equation)
        double F = vessel.AvailableThrust;
        double Isp = vessel.SpecificImpulse * 9.82;
        double m0 = vessel.Mass;
        double m1 = m0 / Math.Exp (deltaV / Isp);
        double flowRate = F / Isp;
        double burnTime = (m0 - m1) / flowRate;

        // Orientate ship
        Console.WriteLine ("Orientating ship for circularization burn");
        vessel.AutoPilot.ReferenceFrame = node.ReferenceFrame;
        vessel.AutoPilot.TargetDirection = Tuple.Create (0.0, 1.0, 0.0);
        vessel.AutoPilot.Wait ();

        // Wait until burn
        Console.WriteLine ("Waiting until circularization burn");
        double burnUT = ut.Get () + vessel.Orbit.TimeToApoapsis - (burnTime / 2.0);
        double leadTime = 5;
        conn.SpaceCenter ().WarpTo (burnUT - leadTime);

        // Execute burn
        Console.WriteLine ("Ready to execute burn");
        var timeToApoapsis = conn.AddStream (() => vessel.Orbit.TimeToApoapsis);
        while (timeToApoapsis.Get () - (burnTime / 2.0) > 0) {
        }
        Console.WriteLine ("Executing burn");
        vessel.Control.Throttle = 1;
        System.Threading.Thread.Sleep ((int)((burnTime - 0.1) * 1000));
        Console.WriteLine ("Fine tuning");
        vessel.Control.Throttle = 0.05f;
        var remainingBurn = conn.AddStream (
            () => node.RemainingBurnVector (node.ReferenceFrame));
        while (remainingBurn.Get ().Item1 > 0) {
        }
        vessel.Control.Throttle = 0;
        node.Remove ();

        Console.WriteLine ("Launch complete");
        conn.Dispose();
    }
}
