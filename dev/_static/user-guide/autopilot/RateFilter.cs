using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class RateFilter {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            // [docs]
            var ap = vessel.AutoPilot;
            // notch a known 1.4 Hz pitch and yaw bending mode from the
            // start
            ap.PitchYawRateFilterMode = RateFilterMode.Notch;
            ap.PitchYawOscillationFrequency = 1.4;
            // [/docs]
        }
    }
}
