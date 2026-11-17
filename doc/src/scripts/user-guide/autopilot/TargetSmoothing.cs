using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class TargetSmoothing {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            // [docs]
            var ap = vessel.AutoPilot;
            ap.ReferenceFrame = vessel.SurfaceReferenceFrame;
            ap.TargetPitchAndHeading (90, 90); // start pointing straight up
            ap.Engaged = true;
            ap.Wait ();

            // ease a 10 degree pitch-over, 90 to 80, in over 20 seconds
            ap.TargetSmoothingTime = 20;
            ap.TargetPitchAndHeading (80, 90);
            ap.Wait (); // returns once the 80 degree target is reached
            // [/docs]
        }
    }
}
