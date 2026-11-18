using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class LimitAngularVelocity {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            // [docs]
            var ap = vessel.AutoPilot;
            // limit the angular velocity by MaxAngularVelocity only
            ap.LimitAngularVelocity = false;
            ap.MaxAngularVelocity = Tuple.Create (0.3, 0.5, 0.3);
            // [/docs]
        }
    }
}
