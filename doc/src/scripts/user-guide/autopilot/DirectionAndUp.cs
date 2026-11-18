using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class DirectionAndUp {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            // [docs]
            var ap = vessel.AutoPilot;
            ap.ReferenceFrame = vessel.SurfaceReferenceFrame;
            // north, in the surface frame, off the flight path
            var up = Tuple.Create (0.0, 1.0, 0.0);

            // straight up at the pad, roof toward north
            ap.SetDirectionAndUp (Tuple.Create (1.0, 0.0, 0.0), up, 0);
            ap.Engaged = true;
            ap.Wait ();

            // ease over to the eastern horizon; roll stays defined through vertical
            for (int i = 1; i <= 90; i++) {
                var pitch = (90 - i) * Math.PI / 180; // 89 down to 0 degrees
                var direction = Tuple.Create (
                    Math.Sin (pitch), 0.0, Math.Cos (pitch));
                ap.SetDirectionAndUp (direction, up, 0);
                ap.Wait ();
            }
            // [/docs]
        }
    }
}
