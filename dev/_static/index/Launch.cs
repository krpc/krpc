using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Launch
{
    public static void Main ()
    {
        var conn = new Connection ("Launch");
        var vessel = conn.SpaceCenter ().ActiveVessel;
        var flight = vessel.Flight ();

        vessel.AutoPilot.TargetPitchAndHeading (90, 90);
        vessel.AutoPilot.Engaged = true;
        vessel.Control.Throttle = 1;
        vessel.Control.ActivateNextStage ();
        Console.WriteLine (vessel.Name + " has launched");

        var evnt = conn.AddEvent (() => flight.MeanAltitude > 10000);
        lock (evnt.Condition) {
            evnt.Wait ();
        }

        vessel.Control.Throttle = 0;
        vessel.AutoPilot.Engaged = false;
        Console.WriteLine (vessel.Name + " reached 10 km");
    }
}
