// [docs]
using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main () {
        using (var connection = new Connection ("pitch hold")) {
            var krpc = connection.KRPC ();
            var vessel = connection.SpaceCenter ().ActiveVessel;
            float targetPitch = 45;

            try {
                for (int i = 0; i < 500; i++) {
                    krpc.NextTick ();
                    var flight = vessel.Flight ();
                    var error = targetPitch - flight.Pitch;
                    vessel.Control.Pitch =
                        Math.Max (-1, Math.Min (1, 0.05f * error));
                }
            } finally {
                krpc.ReleaseTick ();
                vessel.Control.Pitch = 0;
            }
        }
    }
}
// [/docs]
