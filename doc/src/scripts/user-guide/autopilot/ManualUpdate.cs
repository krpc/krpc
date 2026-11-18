using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class ManualUpdate {
    static float ControlLaw (float pitch) {
        return pitch + 0.05f * (45 - pitch);
    }

    static void Record (float error, float pitch) {
        Console.WriteLine ("{0} {1}", error, pitch);
    }

    public static void Main () {
        using (var connection = new Connection ()) {
            var krpc = connection.KRPC ();
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var ap = vessel.AutoPilot;
            ap.Engaged = true;
            // [docs]
            ap.UpdateMode = AutoPilotUpdateMode.Manual;
            while (true) {
                krpc.HoldTick ();
                try {
                    ap.TargetPitchAndHeading (
                        ControlLaw (vessel.Flight ().Pitch), 90);
                    ap.Update ();
                    Record (ap.Error, vessel.Control.Pitch);
                } finally {
                    krpc.ReleaseTick ();
                }
            }
            // [/docs]
        }
    }
}
