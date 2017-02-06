using System;
using System.Collections.Generic;
using System.Net;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class NavballDirections
{
    public static void Main ()
    {
        using (var conn = new Connection ("Orbital directions")) {
            var vessel = conn.SpaceCenter ().ActiveVessel;
            var ap = vessel.AutoPilot;
            ap.ReferenceFrame = vessel.OrbitalReferenceFrame;
            ap.Engage();

            // Point the vessel in the prograde direction
            ap.TargetDirection = Tuple.Create (0.0, 1.0, 0.0);
            ap.Wait();

            // Point the vessel in the orbit normal direction
            ap.TargetDirection = Tuple.Create (0.0, 0.0, 1.0);
            ap.Wait();

            // Point the vessel in the orbit radial direction
            ap.TargetDirection = Tuple.Create (-1.0, 0.0, 0.0);
            ap.Wait();

            ap.Disengage();
        }
    }
}
