using System;
using KRPC.Client;
using KRPC.Client.Services.KSA;

class Launch
{
    public static void Main ()
    {
        var conn = new Connection ("Launch");
        var vehicle = conn.KSA ().LaunchVehicle ("Rocket", "CCSFS LC-39A");
        var flight = vehicle.Flight;

        vehicle.AutoPilot.TargetPitchAndHeading (90, 90);
        vehicle.AutoPilot.Engage ();
        vehicle.Control.Throttle = 1;
        vehicle.Control.ActivateNextSequence ();
        vehicle.Control.EngineOn = true;
        Console.WriteLine (vehicle.Name + " has launched");

        var evnt = conn.AddEvent (() => flight.MeanAltitude > 10000);
        lock (evnt.Condition) {
            evnt.Wait ();
        }

        vehicle.Control.EngineOn = false;
        vehicle.AutoPilot.Disengage ();
        Console.WriteLine (vehicle.Name + " reached 10 km");
    }
}
