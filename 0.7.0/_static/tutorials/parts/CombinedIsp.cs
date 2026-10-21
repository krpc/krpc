using System;
using System.Linq;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class CombinedIsp
{
    public static void Main ()
    {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;

            var activeEngines = vessel.Parts.Engines
                                .Where (e => e.Active && e.HasFuel).ToList ();

            Console.WriteLine ("Active engines:");
            foreach (var engine in activeEngines)
                Console.WriteLine ("   " + engine.Part.Title +
                                   " in stage " + engine.Part.Stage);

            double thrust = activeEngines.Sum (e => e.Thrust);
            double fuel_consumption =
                activeEngines.Sum (e => e.Thrust / e.SpecificImpulse);
            double isp = thrust / fuel_consumption;
            Console.WriteLine ("Combined vacuum Isp = {0:F0} seconds", isp);
        }
    }
}
