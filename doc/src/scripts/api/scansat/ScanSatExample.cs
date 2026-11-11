using System;
using KRPC.Client;
using KRPC.Client.Services.ScanSat;
using KRPC.Client.Services.SpaceCenter;

class ScanSatExample
{
    public static void Main ()
    {
        using (var connection = new Connection ("SCANsat Example")) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var scansat = connection.ScanSat ();

            foreach (var scanner in scansat.Scanners (vessel))
                scanner.Active = true;

            var body = vessel.Orbit.Body;
            var coverage = scansat.Coverage (body, ScanType.Altimetry);
            Console.WriteLine ("Altimetry coverage of {0}: {1:F1}%", body.Name, coverage * 100);

            var flight = vessel.Flight ();
            var elevation = scansat.Elevation (body, flight.Latitude, flight.Longitude);
            if (elevation == null)
                Console.WriteLine ("The terrain below the vessel is not scanned");
            else
                Console.WriteLine ("The terrain below the vessel is at {0:F0} m", elevation.Value);
        }
    }
}
