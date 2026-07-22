using System;
using System.Collections.Generic;
using System.Net;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class NavballDirections
{
    public static void Main ()
    {
        using (var conn = new Connection ("Navball directions")) {
            var vessel = conn.SpaceCenter ().ActiveVessel;
            var ap = vessel.AutoPilot;
            ap.ReferenceFrame = vessel.SurfaceReferenceFrame;
            ap.Engage();

            // Point the vessel north on the navball, with a pitch of 0 degrees
            ap.TargetDirection = Tuple.Create (0.0, 1.0, 0.0);
            ap.Wait();

            // Point the vessel vertically upwards on the navball
            ap.TargetDirection = Tuple.Create (1.0, 0.0, 0.0);
            ap.Wait();

            // Point the vessel west (heading of 270 degrees), with a pitch of 0 degrees
            ap.TargetDirection = Tuple.Create (0.0, 0.0, -1.0);
            ap.Wait();

            ap.Disengage();
        }
    }
}
