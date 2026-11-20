using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class Program {
    static float ControlLaw (float pitch) {
        return 0.05f * (45 - pitch);
    }

    public static void Main () {
        var connection = new Connection ();
        var krpc = connection.KRPC ();
        var vessel = connection.SpaceCenter ().ActiveVessel;
        // [docs]
        while (true) {
            krpc.HoldTick ();
            try {
                var pitch = vessel.Flight ().Pitch;
                vessel.Control.Pitch = ControlLaw (pitch);
            } finally {
                krpc.ReleaseTick ();
            }
        }
        // [/docs]
    }
}
