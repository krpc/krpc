using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using KRPC.Client;
using KRPC.Client.Services.MechJeb;
using KRPC.Client.Services.SpaceCenter;

class LaunchToMinmus
{
    public static void Main ()
    {
        var conn = new Connection ("Launch to Minmus");
        var sc = conn.SpaceCenter ();
        var vessel = sc.ActiveVessel;
        var mj = conn.MechJeb ();

        // Wait until the condition is true
        void WaitFor (Expression<Func<bool>> condition)
        {
            var evnt = conn.AddEvent (condition);
            lock (evnt.Condition) {
                evnt.Wait ();
            }
        }

        // Print each step of the autopilot. Returns the status stream.
        Stream<string> PrintSteps (Expression<Func<string>> autopilotStatus)
        {
            var last = "";
            var status = conn.AddStream (autopilotStatus);
            status.AddCallback ((string value) => {
                // The step, without the figures that change on each update
                var line = value.Split ('\n') [0];
                var step = Regex.Split (line, "[0-9∞]") [0];
                step = step.TrimEnd (' ', ':', '=');
                if (step.Length > 0 && step != last) {
                    last = step;
                    Console.WriteLine (step);
                }
            });
            return status;
        }

        void PrintOrbit ()
        {
            var orbit = vessel.Orbit;
            Console.WriteLine (
                "Orbit of {0}, {1:F0} km by {2:F0} km", orbit.Body.Name,
                orbit.ApoapsisAltitude / 1000,
                orbit.PeriapsisAltitude / 1000);
        }

        // The target orbit, set as in the Ascent Guidance window
        var ascent = mj.AscentGuidance;
        ascent.AscentType = AscentType.Classic;
        ascent.DesiredOrbitAltitude = 100000;
        ascent.DesiredInclination = 0;

        // The Ascent Settings window
        var settings = ascent.Settings;
        settings.Autostage = true;
        settings.LimitAngleOfAttack = true;
        settings.MaxAngleOfAttack = 5;
        settings.Autowarp = true;

        // Print each step of the autopilot
        var status = PrintSteps (() => ascent.Status);

        ascent.Enabled = true;
        vessel.Control.ActivateNextStage ();

        // Wait for the autopilot to disengage, once the orbit is circular
        WaitFor (() => !ascent.Enabled);
        status.Remove ();
        PrintOrbit ();

        var planner = mj.ManeuverPlanner;
        planner.Autowarp = true;

        // Stage once a stage burns out, during the burns too
        mj.Utilities.Staging.Enabled = true;

        // Execute the vessel's maneuver nodes, and wait for the burns
        void ExecuteNodes ()
        {
            planner.ExecuteAllNodes ();
            WaitFor (() => !planner.ExecutingNode);
        }

        // Target Minmus, and wait for MechJeb to see the target
        var minmus = sc.Bodies ["Minmus"];
        sc.TargetBody = minmus;
        var targetController = mj.TargetController;
        WaitFor (() => targetController.NormalTargetExists);

        // Match planes with Minmus, at the nearest relative node
        var plane = planner.OperationPlane;
        plane.TimeSelector.TimeReference = TimeReference.RelNearestAd;
        plane.MakeNodes ();
        ExecuteNodes ();

        // The Hohmann transfer, to an encounter with no capture burn
        var transfer = planner.OperationTransfer;
        transfer.TimeSelector.TimeReference = TimeReference.Computed;
        transfer.Capture = false;
        transfer.MakeNodes ();
        ExecuteNodes ();

        // Fine tune the encounter, for a periapsis 20 km above Minmus
        var correction = planner.OperationCourseCorrection;
        correction.FinalPeriapsisAltitude = 20000;
        correction.MakeNodes ();
        ExecuteNodes ();

        // Warp into Minmus' sphere of influence
        sc.WarpTo (sc.UT + vessel.Orbit.TimeToSOIChange + 60);

        // Circularize at the periapsis
        var circularize = planner.OperationCircularize;
        circularize.TimeSelector.TimeReference = TimeReference.Periapsis;
        circularize.MakeNodes ();
        ExecuteNodes ();

        sc.ClearTarget ();
        PrintOrbit ();

        // Stage until the next stage would drop the landing legs. This
        // drops the stages below the lander, and activates the lander's
        // engine.
        mj.Utilities.Staging.Enabled = false;
        var legs = vessel.Parts.Legs [0].Part;
        while (vessel.Control.CurrentStage > legs.DecoupleStage + 1)
            vessel.Control.ActivateNextStage ();

        // Limit the lander's thrust, so its lowest throttle leaves it
        // falling
        foreach (var engine in vessel.Parts.Engines)
            engine.ThrustLimit = 0.3f;

        // Land on Minmus' Great Flats, and wait for MechJeb to see the
        // target
        var landing = mj.LandingGuidance;
        landing.SetTarget (minmus, 0, -6);
        WaitFor (() => targetController.PositionTargetExists);

        // The settings of the Landing Guidance window
        landing.TouchdownSpeed = 1;
        landing.DeployGears = true;
        landing.Autowarp = true;

        // Print each step of the autopilot
        status = PrintSteps (() => landing.Status);

        landing.LandAtTarget ();

        // Wait for the autopilot to disengage, once the vessel has landed
        WaitFor (() => !landing.Enabled);
        status.Remove ();

        sc.ClearTarget ();
        var flight = vessel.Flight ();
        Console.WriteLine (
            "Landed at latitude {0:F2}, longitude {1:F2}",
            flight.Latitude, flight.Longitude);
        conn.Dispose ();
    }
}
