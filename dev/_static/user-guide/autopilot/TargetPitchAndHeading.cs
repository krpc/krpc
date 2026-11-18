using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class TargetPitchAndHeading {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            // [docs]
            var ap = vessel.AutoPilot;
            ap.ReferenceFrame = vessel.SurfaceReferenceFrame;
            // 80 degrees above the horizon, facing east
            ap.TargetPitchAndHeading (80, 90);
            ap.Engaged = true;
            ap.Wait (); // blocks until the vessel has settled on the target
            ap.Engaged = false;
            // [/docs]
        }
    }
}
